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
        Field<IdGraphType>("create")
            .Description(@"
                Create an analysis request to the analysis engine.
                If the mutation completes successfully, it will return the id of the draft document.
                If any of the repository URL, branch or commit, the mutation will return error code = NOT_FOUND.
            ")
            .Argument<NonNullGraphType<CreateDraftArgumentsGraphType>>("args")
            .Argument<NonNullGraphType<SignatureArgumentsGraphType>>("signature")
            .AuthorizeWithPolicy(PolicyNames.Authenticated)
            .ResolveAsync(CreateDraftAsync);

        Field<IdGraphType>("update")
            .Description(@"
                Update an analysis request to the analysis engine.
                If the mutation completes successfully, it will return the id of the draft document.
                If any of the repository URL, branch or commit, the mutation will return error code = NOT_FOUND.
            ")
            .Argument<NonNullGraphType<UpdateDraftArgumentsGraphType>>("args")
            .AuthorizeWithPolicy(PolicyNames.Authenticated)
            .ResolveAsync(UpdateDraftAsync);
    }

    private async Task<object?> UpdateDraftAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        var args = context.GetValidatedArgument<UpdateDraftArguments>("args");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<Identity.User>(context.User!.Identity!.Name);

        var draft = await session.LoadAsync<JobDraft>(args.Id);

        if (draft.CreatedBy != user.Address)
        {
            return null;
        }

        if (args.Tests != null)
        {
            draft.Tests = args.Tests;
        }

        if (args.Tests != null)
        {
            draft.CreatedBy = user.Address;
        }

        if (args.IsEditable != null)
        {
            draft.IsEditable = args.IsEditable;
        }

        if (args.IsAddable != null)
        {
            draft.IsAddable = args.IsAddable;
        }

        if (args.IsDeletable != null)
        {
            draft.IsDeletable = args.IsDeletable;
        }

        if (args.Funding != null)
        {
            draft.Funding = args.Funding;
        }

        if (args.Name != null)
        {
            draft.Name = args.Name;
        }


        await session.StoreAsync(draft);

        await session.SaveChangesAsync();

        return draft.Id;
    }

    private async Task<object?> CreateDraftAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        var args = context.GetValidatedArgument<CreateDraftArguments>("args");
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
            CreatedBy = user.Address,
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
