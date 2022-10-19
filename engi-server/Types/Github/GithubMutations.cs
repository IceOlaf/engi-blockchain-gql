using Engi.Substrate.Github;
using Engi.Substrate.Identity;
using Engi.Substrate.Server.Github;
using Engi.Substrate.Server.Types.Authentication;
using GraphQL;
using GraphQL.Types;
using Octokit;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Sentry;

namespace Engi.Substrate.Server.Types.Github;

public class GithubMutations : ObjectGraphType
{
    public GithubMutations()
    {
        this.AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<IdGraphType>("enroll")
            .Description("Finish enrollment after a user installs the ENGI app.")
            .Argument<NonNullGraphType<GithubEnrollmentArgumentsGraphType>>("args")
            .ResolveAsync(EnrollAsync);
    }

    private async Task<object?> EnrollAsync(IResolveFieldContext<object?> context)
    {
        var args = context.GetValidatedArgument<GithubEnrollmentArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var sentry = scope.ServiceProvider.GetRequiredService<IHub>();

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
            sentry.CaptureException(ex);

            throw new AuthenticationError();
        }

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<Identity.User>(context.User!.Identity!.Name);

        IReadOnlyList<Repository> repositories;

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
            Repositories = repositories
                .Where(x => !x.Archived)
                .Select(x => new GithubRepository
                {
                    Id = x.Id,
                    Name = x.Name,
                    FullName = x.FullName,
                    IsPrivate = x.Private,
                    OwnerAvatarUrl = installation.Account.AvatarUrl
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
}