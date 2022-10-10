using System.Text.Json;
using System.Text.Json.Serialization;
using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Types.Authentication;
using GraphQL;
using GraphQL.Types;
using Octokit;
using Raven.Client.Documents.Session;
using Sentry;
using Language = Engi.Substrate.Jobs.Language;
using Repository = Octokit.Repository;

namespace Engi.Substrate.Server.Types.Analysis;

public class AnalysisMutations : ObjectGraphType
{
    public AnalysisMutations()
    {
        Field<IdGraphType>("submit")
            .Description(@"
                Submit an analysis request to the analysis engine. 
                If the mutation completes successfully, it will return the id of the analysis document. 
                If any of the repository URL, branch or commit, the mutation will return error code = NOT_FOUND.
            ")
            .Argument<NonNullGraphType<SubmitAnalysisArgumentsGraphType>>("args")
            .Argument<NonNullGraphType<SignatureArgumentsGraphType>>("signature")
            .AuthorizeWithPolicy(PolicyNames.Authenticated)
            .ResolveAsync(SubmitAnalysisAsync);

        Field<IdGraphType>("submitUpdate")
            .Description(@"
                Submit an analysis update to an existing job (Sudo).
                If the job is not found, the mutation will return error code = NOT_FOUND.
            ")
            .Argument<NonNullGraphType<SubmitAnalysisUpdateArgumentsGraphType>>("args")
            .AuthorizeWithPolicy(PolicyNames.Sudo)
            .ResolveAsync(SubmitUpdateAsync);
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
            Commit = commit.Sha,
            CreatedBy = context.User!.Identity!.Name!
        };

        await session.StoreAsync(analysis);

        await session.SaveChangesAsync();

        return analysis.Id;
    }

    public async Task<object?> SubmitUpdateAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        var args = context.GetValidatedArgument<SubmitAnalysisUpdateArguments>("args");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var analysis = await session.LoadAsync<RepositoryAnalysis>(args.Id);

        if (analysis == null)
        {
            throw new ExecutionError("Analysis was not found.") { Code = "NOT_FOUND" };
        }

        analysis.ExecutionResult = new()
        {
            Stdout = args.Stdout,
            Stderr = args.Stderr,
            ReturnCode = args.ReturnCode
        };

        analysis.Status = analysis.ExecutionResult.ReturnCode == 0
            ? RepositoryAnalysisStatus.Completed
            : RepositoryAnalysisStatus.Failed;

        if (analysis.Status == RepositoryAnalysisStatus.Completed)
        {
            try
            {
                var payload = JsonSerializer
                    .Deserialize<AnalysisPayload>(analysis.ExecutionResult.Stdout, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    })!;

                analysis.Language = payload.Language;
                analysis.Files = payload.Files;
                analysis.Complexity = payload.Complexity;
                analysis.Tests = payload.Tests;
            }
            catch (Exception ex)
            {
                var sentry = scope.ServiceProvider.GetRequiredService<IHub>();

                sentry.CaptureException(ex);
            }
        }

        await session.SaveChangesAsync();

        return null;
    }

    class AnalysisPayload
    {
        public Language Language { get; set; }

        public string[]? Files { get; set; }

        public RepositoryComplexity? Complexity { get; set; }

        public TestAttempt[]? Tests { get; set; }
    }
}