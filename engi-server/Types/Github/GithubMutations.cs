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
    }

    private async Task<object?> EnrollAsync(IResolveFieldContext<object?> context)
    {
        var args = context.GetValidatedArgument<GithubEnrollmentArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

        long installationId = long.Parse(args.InstallationId);

        var logger = loggerFactory.CreateLogger<GithubMutations>();

        logger.LogInformation($"Enrolling installation {installationId}");

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
            logger.LogError(ex, "Enrollment could not be verified.");

            throw new AuthenticationError();
        }

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        var displayName = user.Display;
        logger.LogInformation($"User is {displayName}");

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

        if (enrollment == null)
        {
            logger.LogInformation("Enrollment is null");
        }

        session.Advanced.Clear();

        session.Advanced.Defer(
            new PatchCommandData(
                user.Id, null, new UpdateGithubEnrollmentPatchRequest(enrollment)));

        var githubAppInstallationUserReference = new GithubAppInstallationUserReference(installation, user);

        await session.StoreAsync(githubAppInstallationUserReference, null, githubAppInstallationUserReference.Id);

        await session.SaveChangesAsync();

        return null;
    }
}
