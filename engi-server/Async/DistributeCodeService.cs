using System.Text;
using Engi.Substrate.Identity;
using Engi.Substrate.Indexing;
using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Github;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Options;
using Octokit;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Sentry;
using Constants = Raven.Client.Constants;
using FileMode = System.IO.FileMode;
using NotFoundException = Octokit.NotFoundException;
using Repository = LibGit2Sharp.Repository;
using User = Engi.Substrate.Identity.User;

namespace Engi.Substrate.Server.Async;

public class DistributeCodeService : SubscriptionProcessingBase<DistributeCodeCommand>
{
    private readonly IOptionsMonitor<ApplicationOptions> applicationOptions;
    private readonly IOptionsMonitor<SubstrateClientOptions> substrateOptions;

    public DistributeCodeService(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        IHub sentry,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<ApplicationOptions> applicationOptions,
        IOptions<EngiOptions> engiOptions,
        IOptionsMonitor<SubstrateClientOptions> substrateOptions)
        : base(store, serviceProvider, sentry, engiOptions, loggerFactory)
    {
        this.applicationOptions = applicationOptions;
        this.substrateOptions = substrateOptions;

        MaxDocumentsPerBatch = engiOptions.Value.ProcessRavenSubscriptionsMaxDocumentPerEngineBatch;
    }

    protected override string CreateQuery()
    {
        return @"
            declare function filter(b) {
                return b.ProcessedOn === null && b.SentryId === null
            }

            from DistributeCodeCommands as c where filter(c)
        ";
    }

    protected override async Task ProcessBatchAsync(SubscriptionBatch<DistributeCodeCommand> batch, IServiceProvider serviceProvider)
    {
        using var session = batch.OpenAsyncSession();

        foreach (var item in batch.Items)
        {
            var command = item.Result;

            try
            {
                string? prUrl = await ProcessAsync(command, session, serviceProvider);

                if (prUrl != null)
                {
                    command.PullRequestUrl = prUrl;
                    command.ProcessedOn = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                command.SentryId = Sentry.CaptureException(ex, new()
                {
                    ["command"] = command.Id
                }).ToString();

                Logger.LogWarning(ex,
                    "Processing command {command} failed; sentry id={sentryId}.",
                    command.Id, command.SentryId);
            }

            await session.SaveChangesAsync();
        }
    }

    private async Task<string?> ProcessAsync(DistributeCodeCommand command,
        IAsyncDocumentSession session,
        IServiceProvider serviceProvider)
    {
        // get job, which should be solved

        var jobReference = await session
            .LoadAsync<ReduceOutputReference>(JobIndex.ReferenceKeyFrom(command.JobId),
                include => include.IncludeDocuments(x => x.ReduceOutputs));

        var job = await session.LoadAsync<Job>(jobReference.ReduceOutputs.First());

        // if job is not solved, assume it's because indexing hasn't caught up

        if (job.Solution == null)
        {
            // don't defer for ever, check time elapsed if previously deferred

            if (command.FirstDeferredOn.HasValue)
            {
                var elapsed = DateTime.UtcNow - command.FirstDeferredOn.Value;

                if (elapsed.TotalHours > 1)
                {
                    throw new InvalidOperationException(
                        "Time out waiting for solution/job to appear.");
                }
            }

            command.FirstDeferredOn ??= DateTime.UtcNow;

            Logger.LogWarning(
                "Job or solution was not found, deferring; job={jobId} solution={solutionId}",
                command.JobId, command.SolutionId);

            var meta = session.Advanced.GetMetadataFor(command);

            meta[Constants.Documents.Metadata.Refresh] = DateTime.UtcNow.AddMinutes(1);

            return null;
        }

        if (job.Solution.SolutionId != command.SolutionId)
        {
            throw new InvalidOperationException("Job solution id mismatch.");
        }

        // get participants

        string creatorReferenceId = UserAddressReference.KeyFrom(job.Creator);
        string authorReferenceId = UserAddressReference.KeyFrom(job.Solution.Author);

        var participantReferences = await session
            .LoadAsync<UserAddressReference>(
                new[]
                {
                    creatorReferenceId,
                    authorReferenceId
                },
                include => include.IncludeDocuments(x => x.UserId));

        var creator = await session.LoadAsync<User>(participantReferences[creatorReferenceId].UserId);
        var author = await session.LoadAsync<User>(participantReferences[authorReferenceId].UserId);

        var octokitFactory = serviceProvider.GetRequiredService<GithubClientFactory>();

        string workBranchName = $"engi-solution/{job.Solution.SolutionId}";
        string workBranchRef = $"refs/heads/{workBranchName}";

        var (enrollment, targetRepoReference) = creator.GithubEnrollments
            .Find(job.Repository.Organization, job.Repository.Name);

        var octokit = await octokitFactory.CreateForAsync(enrollment!.InstallationId);

        var targetRepo = await octokit.Repository.Get(targetRepoReference!.Id);

        // check if branch already exists and delete it

        try
        {
            await octokit.Repository.Branch.Get(targetRepo.Id, workBranchRef);

            // this means the branch exists, in which case, check if PR exists also
            // if it exists, throw. doing it like this covers both:
            // - command doesnt delete stuff if it runs twice for some reason
            // - if it fails, we can delete the branch and it'll do it again

            var prs = await octokit.PullRequest.GetAllForRepository(targetRepo.Id, new PullRequestRequest
            {
                Head = workBranchRef
            });

            if (prs.Any())
            {
                throw new InvalidOperationException(
                    $"Repository already has a PR from {workBranchRef}");
            }

            await octokit.Git.Reference.Delete(targetRepo.Id, workBranchRef);
        }
        catch (NotFoundException)
        {
            // good
        }

        // clone repository

        using var workDirectory = new DisposableRepositoryDirectory();

        CredentialsHandler credentialsProvider = (_, _, _) => new UsernamePasswordCredentials
        {
            Username = "engi-bot",
            Password = octokit.Connection.Credentials.GetToken()
        };

        Repository.Clone(job.Repository.Url, workDirectory.RepoPath, new()
        {
            BranchName = job.Repository.Branch,
            CredentialsProvider = credentialsProvider
        });

        string title = GetPullRequestTitle(job);

        using (var localRepo = new Repository(workDirectory.RepoPath))
        {
            // create branch and add remote

            var workBranch = localRepo.CreateBranch(workBranchName, job.Repository.Branch);

            localRepo.Network.Remotes.Add("target", targetRepo.CloneUrl);

            localRepo.Branches.Update(workBranch, branch =>
            {
                branch.Remote = "target";
                branch.UpstreamBranch = workBranchRef;
            });

            // checkout and apply patch

            Commands.Checkout(localRepo, workBranch);

            await DownloadAndWritePatchToFileAsync(
                serviceProvider, job.Solution.PatchUrl, workDirectory.PatchPath);

            localRepo.ApplyPatchFile(workDirectory.PatchPath, new PatchApplyOptions
            {
                Location = PatchApplyLocation.Both
            });

            // create commit + push

            var now = DateTimeOffset.UtcNow;

            localRepo.Commit(title,
                new LibGit2Sharp.Signature(author.Display, author.Email, now),
                // this doesn't seem to matter, it gets replaced by GH
                new LibGit2Sharp.Signature("engi-bot", "bot@engi.network", now),
                new CommitOptions { PrettifyMessage = true });

            localRepo.Network.Push(workBranch, new PushOptions
            {
                CredentialsProvider = credentialsProvider
            });
        }

        // open PR

        var block = await session
            .LoadAsync<ExpandedBlock>(ExpandedBlock.KeyFrom(job.UpdatedOn.Number));

        var request = new NewPullRequest(title,
            workBranchName,
            $"refs/heads/{job.Repository.Branch}")
        {
            Body = await octokit.Markdown.RenderArbitraryMarkdown(
                new(GetPullRequestBody(job, job.Solution, author, block)))
        };

        var pr = await octokit.PullRequest
            .Create(targetRepo.Id, request);

        return pr.Url;
    }

    private async Task DownloadAndWritePatchToFileAsync(
        IServiceProvider serviceProvider,
        string patchFile,
        string outputPath)
    {
        var http = serviceProvider.GetRequiredService<HttpClient>();

        // copy directly from response to tmp file

        string tmp = Path.GetTempFileName();

        var patchContentStream = await http.GetStreamAsync(patchFile);

        // determine UTF encoding from BOM, seems to be UTF-16
        // for diggs generated from Powershell

        bool isUtf16 = false;

        await using (var fileStream = File.Open(tmp, FileMode.Create, FileAccess.ReadWrite))
        {
            await patchContentStream.CopyToAsync(fileStream);

            fileStream.Seek(0, SeekOrigin.Begin);

            byte[] bom = new byte[2];

            if (await fileStream.ReadAsync(bom, 0, 2) == 2
                && bom[0] == 0xFF && bom[1] == 0xFE)
            {
                isUtf16 = true;
            }
        }

        // convert to UTF8, LF format so git can digest this

        await ConvertPatchFileAsync(tmp, isUtf16 ? Encoding.Unicode : Encoding.UTF8, outputPath);
    }

    private static async Task ConvertPatchFileAsync(string inputPath, Encoding encoding, string outputPath)
    {
        using StreamReader reader = new StreamReader(inputPath, encoding);

        await using FileStream writer = File.OpenWrite(outputPath);

        char[] buffer = new char[8192];

        int len;
        while ((len = await reader.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            string unicode = new string(buffer, 0, len).Replace("\r\n", "\n");
            byte[] utf8 = Encoding.UTF8.GetBytes(unicode);

            await writer.WriteAsync(utf8, 0, utf8.Length);
        }
    }

    private static string GetPullRequestTitle(Job job)
    {
        return $"👾 Completed Engi Job: {job.Name}";
    }

    private string GetPullRequestBody(Job job, Solution solution, User solver, ExpandedBlock block)
    {
        string baseUrl = applicationOptions.CurrentValue.Url;
        string explorerUrl =
            $"https://polkadot.js.org/apps/?rpc={Uri.EscapeDataString(substrateOptions.CurrentValue.WsUrl)}#/explorer/query/{block.Hash}";

        return $@"Job [#{job.JobId}]({baseUrl}/jobs/{job.JobId}) was solved by [{solver.Display}]({baseUrl}/account/{solver.Address})\
\
Solution #{solution.SolutionId}\
\
Block #{block.Number} [{block.Hash}]({explorerUrl})";
    }

    class DisposableRepositoryDirectory : IDisposable
    {
        public string BasePath { get; }

        public string RepoPath { get; }

        public string PatchPath { get; }

        public DisposableRepositoryDirectory()
        {
            BasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            RepoPath = Path.Combine(BasePath, "repo");
            PatchPath = Path.Combine(BasePath, "solution.patch");

            Directory.CreateDirectory(BasePath);
            Directory.CreateDirectory(RepoPath);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(BasePath, true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
