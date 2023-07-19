using Engi.Substrate.Github;
using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Github;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Sentry;
using User = Engi.Substrate.Identity.User;

namespace Engi.Substrate.Server.Async;

public class RetrieveGithubReadmesService : SubscriptionProcessingBase<JobSnapshot>
{
    public RetrieveGithubReadmesService(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        IHub sentry,
        IOptions<EngiOptions> engiOptions,
        ILoggerFactory loggerFactory)
        : base(store, serviceProvider, sentry, engiOptions, loggerFactory)
    { }

    protected override string CreateQuery()
    {
        return @"
            declare function filter(snapshot) {
                return snapshot.IsCreation === true
            }

            from JobSnapshots as snapshot where filter(snapshot)
        ";
    }

    protected override async Task ProcessBatchAsync(SubscriptionBatch<JobSnapshot> batch, IServiceProvider serviceProvider)
    {
        using var session = batch.OpenAsyncSession();

        foreach (var item in batch.Items)
        {
            var command = item.Result;

            try
            {
                await ProcessAsync(command, session, serviceProvider);
            }
            catch (Exception ex)
            {
                var sentryId = Sentry.CaptureException(ex, new()
                {
                    ["command"] = command.Id
                });

                Logger.LogWarning(ex,
                    "Processing command {command} failed; sentry id={sentryId}.",
                    command.Id, sentryId.ToString());
            }

            await session.SaveChangesAsync();
        }
    }

    private async Task ProcessAsync(
        JobSnapshot job,
        IAsyncDocumentSession session,
        IServiceProvider serviceProvider)
    {
        string creatorReferenceId = UserAddressReference.KeyFrom(job.Creator);
        string readmeId = GithubRepositoryReadme.KeyFrom(job.Repository.FullName);

        var creatorReferenceLazy = session
            .Advanced.Lazily
            .Include<UserAddressReference>(x => x.UserId)
            .LoadAsync<UserAddressReference>(creatorReferenceId);

        var readmeLazy = session
            .Advanced.Lazily
            .LoadAsync<GithubRepositoryReadme>(readmeId);

        await session.Advanced.Eagerly
            .ExecuteAllPendingLazyOperationsAsync();

        var readme = readmeLazy.Value.Result;

        if (readme != null && (DateTime.UtcNow - readme.RetrievedOn).TotalHours < 1)
        {
            return; // we already have one and it's recent
        }

        var creatorId = creatorReferenceLazy.Value.Result;

        if (creatorId == null)
        {
            throw new ChainAssumptionInconsistencyException(
                $"A user matching address={job.Creator} was not found.");
        }

        var creator = await session.LoadAsync<User>(
            creatorReferenceLazy.Value.Result.UserId);

        var (enrollment, repo) = creator.GithubEnrollments
            .Find(job.Repository.FullName);

        if (readme == null)
        {
            readme = new GithubRepositoryReadme
            {
                Id = GithubRepositoryReadme.KeyFrom(job.Repository.FullName)
            };

            await session.StoreAsync(readme);
        }

        var octokitFactory = serviceProvider.GetRequiredService<GithubClientFactory>();

        var octokit = await octokitFactory.CreateForAsync(enrollment!.InstallationId);

        var githubRepo = await octokit.Repository.Get(repo!.Id);

        var content = await octokit.Repository.Content.GetReadme(repo!.Id);

        readme.Owner = new GithubRepositoryOwner
        {
            Login = githubRepo.Owner.Login,
            AvatarUrl = githubRepo.Owner.AvatarUrl
        };

        readme.RetrievedOn = DateTime.UtcNow;
        readme.Content = content.Content;

        var analysis = new RepositoryAnalysis
        {
            JobId = job.JobId,
            RepositoryUrl = job.Repository.Url,
            Branch = job.Repository.Branch,
            Commit = job.Repository.Commit,
            CreatedBy = job.Creator
        };

        await session.StoreAsync(analysis);

        await session.StoreAsync(new QueueEngineRequestCommand {
            Identifier = analysis.Id,
            CommandString = $"analyse {analysis.RepositoryUrl} --branch {analysis.Branch} --commit {analysis.Commit}",
            SourceId = analysis.Id
        });
    }
}
