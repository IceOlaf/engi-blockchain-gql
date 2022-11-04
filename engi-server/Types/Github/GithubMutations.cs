using Engi.Substrate.Github;
using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Async;
using Engi.Substrate.Server.Github;
using Engi.Substrate.Server.Types.Authentication;
using GraphQL;
using GraphQL.Types;
using Octokit;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Session;
using Sentry;
using User = Engi.Substrate.Identity.User;

namespace Engi.Substrate.Server.Types.Github;

public class GithubMutations : ObjectGraphType
{
    public GithubMutations()
    {
        Field<IdGraphType>("enroll")
            .Description("Finish enrollment after a user installs the ENGI app.")
            .Argument<NonNullGraphType<GithubEnrollmentArgumentsGraphType>>("args")
            .ResolveAsync(EnrollAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<StringGraphType>("distributeCode")
            .Description("Distribute the code for a job by opening a PR to the source repository, with the completed job's patch.")
            .Argument<NonNullGraphType<DistributeCompletedJobCodeArgumentsGraphType>>("args")
            .ResolveAsync(DistributeCodeAsync)
            .AuthorizeWithPolicy(PolicyNames.Sudo);
    }

    private async Task<object?> EnrollAsync(IResolveFieldContext<object?> context)
    {
        var args = context.GetValidatedArgument<GithubEnrollmentArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

        long installationId = long.Parse(args.InstallationId);

        var octokitFactory = scope.ServiceProvider.GetRequiredService<GithubClientFactory>();

        Installation installation;
        GitHubClient octokit;

        try
        {
            octokit = octokitFactory.Create();

            installation = await octokit.GitHubApps.GetInstallationForCurrent(installationId);

            octokit = await octokitFactory.SpecializeForAsync(octokit, installation.Id);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<GithubMutations>();

            logger.LogError(ex, "Enrollment could not be verified.");

            throw new AuthenticationError();
        }

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        IReadOnlyList<Octokit.Repository> repositories;

        if (installation.TargetType.Value == AccountType.User)
        {
            var response = await octokit.GitHubApps.Installation.GetAllRepositoriesForCurrent();

            repositories = response.Repositories;
        }
        else
        {
            repositories = await octokit.Repository.GetAllForOrg(installation.Account.Login);
        }

        var enrollment = new UserGithubEnrollment
        {
            InstallationId = installation.Id,
            Owner = new()
            {
                Login = installation.Account.Login,
                AvatarUrl = installation.Account.AvatarUrl,
            },
            Repositories = repositories
                .Where(x => !x.Archived)
                .Select(x => new GithubRepository
                {
                    Id = x.Id,
                    Name = x.Name,
                    FullName = x.FullName,
                    IsPrivate = x.Private
                })
                .ToList()
        };

        session.Advanced.Defer(
            new PatchCommandData(
                user.Id, null, new UpdateGithubEnrollmentPatchRequest(enrollment)));

        var githubAppInstallationUserReference = new GithubAppInstallationUserReference(installation, user);

        await session.StoreAsync(githubAppInstallationUserReference, null, githubAppInstallationUserReference.Id);

        await session.SaveChangesAsync();

        return null;
    }

    private async Task<object?> DistributeCodeAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<DistributeCompletedJobCodeArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        // Ideally, when this called is made, the job will be solved. However, since
        // both jobs and solutions are indexed first from the chain and then
        // from Raven, there is a delay in this so it would be a race condition.
        // Since this call is made from a trusted service, we'll only verify the job exists

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var job = await session
            .LoadAsync<ReduceOutputReference>(JobIndex.ReferenceKeyFrom(args.JobId));

        if (job == null)
        {
            throw new ExecutionError("Job not found.") { Code = "NOT_FOUND" };
        }

        await session.StoreAsync(new DistributeCodeCommand
        {
            JobId = args.JobId,
            SolutionId = args.SolutionId
        });

        await session.SaveChangesAsync();

        return null;
    }
}
