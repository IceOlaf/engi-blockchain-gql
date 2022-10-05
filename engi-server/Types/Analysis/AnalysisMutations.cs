﻿using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Types.Authentication;
using GraphQL;
using GraphQL.Types;
using Octokit;
using Raven.Client.Documents.Session;
using Repository = Octokit.Repository;

namespace Engi.Substrate.Server.Types.Analysis;

public class AnalysisMutations : ObjectGraphType
{
    public AnalysisMutations()
    {
        this.AuthorizeWithPolicy(PolicyNames.Sudo);

        Field<IdGraphType>("submit")
            .Description(@"
                Submit an analysis request to the analysis engine (Sudo). 
                If the mutation completes successfully, it will return the id of the analysis document. 
                If any of the repository URL, branch or commit, the mutation will return error code = NOT_FOUND.
            ")
            .Argument<NonNullGraphType<SubmitAnalysisArgumentsGraphType>>("args")
            .Argument<NonNullGraphType<SignatureArgumentsGraphType>>("signature")
            .ResolveAsync(SubmitAnalysisAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);
    }

    private async Task<object?> SubmitAnalysisAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        var args = context.GetValidatedArgument<SubmitAnalysisArguments>("args");
        var signature = context.GetValidatedArgument<SignatureArguments>("signature");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var crypto = scope.ServiceProvider.GetRequiredService<UserCryptographyService>();

        var user = await session.LoadAsync<Identity.User>(context.User!.Identity!.Name);

        crypto.ValidateOrThrow(user, signature);

        var octokit = scope.ServiceProvider.GetRequiredService<GitHubClient>();

        var (organization, name) = RepositoryUrl.Parse(args.Url);

        Repository repository;
        GitHubCommit commit;

        try
        {
            repository = await octokit.Repository.Get(organization, name);

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
            RepositoryUrl = args.Url,
            Branch = args.Branch,
            Commit = commit.Sha
        };

        await session.StoreAsync(analysis);

        await session.SaveChangesAsync();

        return analysis.Id;
    }
}