using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Async;
using Engi.Substrate.Server.Github;
using Engi.Substrate.Server.Types.Authentication;
using GraphQL;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Types;
using Octokit;
using Raven.Client.Documents.Session;
using Repository = Octokit.Repository;

namespace Engi.Substrate.Server.Types.Engine;

public class DraftMutations : ObjectGraphType
{
    public DraftMutations()
    {
        Field<IdGraphType>("submit")
            .Description(@"
                Submit an analysis request to the analysis engine.
                If the mutation completes successfully, it will return the id of the draft document.
                If any of the repository URL, branch or commit, the mutation will return error code = NOT_FOUND.
            ")
            .Argument<NonNullGraphType<SubmitDraftArgumentsGraphType>>("args")
            .Argument<NonNullGraphType<SignatureArgumentsGraphType>>("signature")
            .AuthorizeWithPolicy(PolicyNames.Authenticated)
            .ResolveAsync(SubmitDraftAsync);
    }

    private async Task<object?> SubmitDraftAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        var args = context.GetValidatedArgument<SubmitDraftArguments>("args");
        var signature = context.GetValidatedArgument<SignatureArguments>("signature");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var crypto = scope.ServiceProvider.GetRequiredService<UserCryptographyService>();

        var user = await session.LoadAsync<Identity.User>(context.User!.Identity!.Name);

        crypto.ValidateOrThrow(user, signature);

        string repositoryFullName = RepositoryUrl.ParseFullName(args.Url);

        var (enrollment, repoReference) = user.GithubEnrollments.Find(repositoryFullName);

        if (enrollment == null)
        {
            throw new AccessDeniedError(args.Url);
        }

        Repository repository;
        GitHubCommit commit;

        var octokitFactory = scope.ServiceProvider.GetRequiredService<GithubClientFactory>();
        var octokit = await octokitFactory.CreateForAsync(enrollment.InstallationId);

        try
        {
            repository = await octokit.Repository.Get(repoReference!.Id);

            await octokit.Repository.Branch.Get(repository.Id, args.Branch);
        }
        catch (NotFoundException)
        {
            throw new ExecutionError("Repository, branch or commit not found.") { Code = "NOT_FOUND" };
        }

        try
        {
            commit = await octokit.Repository.Commit.Get(repository.Id, args.Commit);
        }
        catch (ApiValidationException)
        {
            throw new ExecutionError("Repository, branch or commit not found.") { Code = "NOT_FOUND" };
        }

        var analysis = new RepositoryAnalysis
        {
            JobId = 0,
            RepositoryUrl = args.Url,
            Branch = args.Branch,
            Commit = commit.Sha,
            CreatedBy = user.Address
        };

        await session.StoreAsync(analysis);

        var draft = new JobDraft
        {
            Tests = args.Tests,
            CreatedBy = user.Address,
            IsEditable = args.IsEditable,
            IsAddable = args.IsAddable,
            IsDeletable = args.IsDeletable,
            Funding = args.Funding,
            Name = args.Name,
            AnalysisId = analysis.Id,
        };

        await session.StoreAsync(draft);

        await session.StoreAsync(new QueueEngineRequestCommand
        {
            Identifier = draft.Id,
            CommandString = $"analyse {analysis.RepositoryUrl} --branch {analysis.Branch} --commit {analysis.Commit}",
            SourceId = analysis.Id
        });

        await session.SaveChangesAsync();

        return draft.Id;
    }
}
