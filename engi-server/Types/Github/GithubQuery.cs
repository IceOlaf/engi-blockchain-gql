using Engi.Substrate.Github;
using Engi.Substrate.Server.Github;
using Engi.Substrate.Server.Types.Validation;
using GraphQL;
using GraphQL.Types;
using Octokit;
using Raven.Client.Documents.Session;

using Repository = Octokit.Repository;
using User = Engi.Substrate.Identity.User;

namespace Engi.Substrate.Server.Types.Github;

public class GithubQuery : ObjectGraphType
{
    public GithubQuery()
    {
        Field<ListGraphType<GithubRepositoryWithOwnerGraphType>>("repositories")
            .Description("Get all repositories that the app installation gives us access to.")
            .ResolveAsync(GetRepositoriesAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<ListGraphType<StringGraphType>>("branches")
            .Description("Get branches for a repository the user has access to.")
            .Argument<NonNullGraphType<StringGraphType>>("repositoryUrl")
            .ResolveAsync(GetBranchesAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<ListGraphType<CommitGraphType>>("commits")
            .Description("Get commits for a repository/branch the user has access to.")
            .Argument<NonNullGraphType<StringGraphType>>("repositoryUrl")
            .Argument<NonNullGraphType<StringGraphType>>("branch")
            .ResolveAsync(GetCommitsAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<UserGithubEnrollmentGraphType>("repositoryAuthorization")
            .Description("Get GitHub authorization for a user/repository pair.")
            .Argument<NonNullGraphType<StringGraphType>>("userId")
            .Argument<NonNullGraphType<StringGraphType>>("repositoryUrl")
            .ResolveAsync(GetAuthorizationForRepositoryAsync)
            .AuthorizeWithPolicy(PolicyNames.Sudo);

        Field<StringGraphType>("patchfile")
            .Description("Return the patchfile for the specified repositories.")
            .Argument<NonNullGraphType<PatchFileRequestArgumentsGraphType>>("args")
            .ResolveAsync(GetPatchFile)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);
    }

    private async Task<object?> GetPatchFile(IResolveFieldContext<object?> context)
    {
        var args = context.GetArgument<PatchFileRequestArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var (baseRepoOwner, baseRepoName) = ParseOrThrowRepositoryUrl(args.BaseRepositoryUrl);
        var (forkRepoOwner, forkRepoName) = ParseOrThrowRepositoryUrl(args.ForkRepositoryUrl);

        var octokitFactory =
            scope.ServiceProvider.GetRequiredService<GithubClientFactory>();
        var http = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        string response = null;

        if (user.GithubEnrollments.Any())
        {
            var (matchingEnrollment, matchingRepo) = user.GithubEnrollments.Find(forkRepoOwner, forkRepoName);

            if (matchingEnrollment != null)
            {
                var octokit = await octokitFactory.CreateForAsync(matchingEnrollment.InstallationId);

                string @base = $"{baseRepoOwner}:{args.BaseRepositoryCommit}";
                string head = $"{forkRepoOwner}:{args.ForkRepositoryCommit}";

                var compare_result = await octokit.Repository.Commit.Compare(baseRepoOwner, baseRepoName, @base, head);

                var gist = new NewGist();
                var now = DateTime.UtcNow;
                string filename = $"{@base}-{head}-{now}";
                string content = http.GetStringAsync(compare_result.PatchUrl).Result;

                gist.Files[filename] = content;

                var newGist = await octokit.Gist.Create(gist);

                response = newGist.Url;
            }
        }

        return response;
    }

    private async Task<object?> GetBranchesAsync(IResolveFieldContext<object?> context)
    {
        string repositoryUrl = context.GetArgument<string>("repositoryUrl");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        var (repoOwner, repoName) = ParseOrThrowRepositoryUrl(repositoryUrl);

        var octokitFactory =
            scope.ServiceProvider.GetRequiredService<GithubClientFactory>();

        IReadOnlyList<Branch>? branches = null;

        if (user.GithubEnrollments.Any())
        {
            var (matchingEnrollment, matchingRepo) = user.GithubEnrollments.Find(repoOwner, repoName);

            if (matchingEnrollment != null)
            {
                var octokit = await octokitFactory.CreateForAsync(matchingEnrollment.InstallationId);

                branches = await octokit.Repository.Branch.GetAll(matchingRepo!.Id);
            }
        }

        if (branches == null)
        {
            // try as a public repo

            var octokit = octokitFactory.CreateAnonymous();

            try
            {
                branches = await octokit.Repository.Branch.GetAll(repoOwner, repoName);
            }
            catch (NotFoundException)
            {
                throw new ExecutionError("Repository not found or user hasn't given access to it.")
                {
                    Code = "NOT_FOUND"
                };
            }
        }

        return branches
            .Select(x => x.Name);
    }

    private static (string owner, string name) ParseOrThrowRepositoryUrl(string repositoryUrl)
    {
        try
        {
            return RepositoryUrl.Parse(repositoryUrl);
        }
        catch (ArgumentException)
        {
            throw new ArgumentValidationException(nameof(repositoryUrl), "Invalid or unsupported repository URL");
        }
    }

    private async Task<object?> GetCommitsAsync(IResolveFieldContext<object?> context)
    {
        string repositoryUrl = context.GetArgument<string>("repositoryUrl");
        string branch = context.GetArgument<string>("branch");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var (repoOwner, repoName) = ParseOrThrowRepositoryUrl(repositoryUrl);

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RootSchema>>();

        var octokitFactory = scope.ServiceProvider.GetRequiredService<GithubClientFactory>();

        IReadOnlyList<GitHubCommit>? commits = null;

        if (user.GithubEnrollments.Any())
        {
            var (matchingEnrollment, matchingRepo) = user.GithubEnrollments.Find(repoOwner, repoName);

            if (matchingEnrollment != null)
            {
                var octokit = await octokitFactory.CreateForAsync(matchingEnrollment.InstallationId);

                commits = await octokit.Repository.Commit.GetAll(matchingRepo!.Id, new CommitRequest { Sha = branch });
                logger.LogInformation($"Commits fetched for {repoOwner} {repoName}.");
            }
            else
            {
                logger.LogInformation($"No matching enrollment for {repoOwner} {repoName}.");
            }
        }

        if (commits == null)
        {
            logger.LogInformation($"No matching enrollment for {repoOwner} {repoName}, trying as public repo.");

            // try as a public repo

            var octokit = octokitFactory.CreateAnonymous();

            Repository repository;

            try
            {
                repository = await octokit.Repository.Get(repoOwner, repoName);
            }
            catch (NotFoundException)
            {
                throw new ExecutionError("Repository not found or user hasn't given access to it.")
                {
                    Code = "NOT_FOUND"
                };
            }

            commits = await octokit.Repository.Commit.GetAll(repository.Id, new CommitRequest { Sha = branch });
        }

        if (commits == null)
        {
            throw new ExecutionError($"Failed to fetch commits for {repoOwner} {repoName}; no matching enrollment found and anonymous access failed.")
            {
                Code = "NOT_FOUND"
            };
        }

        return commits
            .Select(x => new Commit
            {
                Sha = x.Sha,
                Message = x.Commit.Message,
                Author = x.Author.Login,
                Committer = x.Committer.Login
            });
    }

    private async Task<object?> GetRepositoriesAsync(IResolveFieldContext<object?> context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        ThrowIfUserNotEnrolled(user);

        return user.GithubEnrollments
            .Values
            .SelectMany(x => x.Repositories.Select(repo => new GithubRepositoryWithOwner
            {
                Id = repo.Id,
                Name = repo.Name,
                FullName = repo.FullName,
                IsPrivate = repo.IsPrivate,
                Owner = x.Owner
            }));
    }

    private async Task<object?> GetAuthorizationForRepositoryAsync(IResolveFieldContext<object?> context)
    {
        string userId = context.GetArgument<string>("userId");
        string repositoryUrl = context.GetArgument<string>("repositoryUrl");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(userId);

        if (user == null)
        {
            throw new InvalidOperationException(
                $"Invalid engine request to get github authorization for user id={userId}; user does not exist.");
        }

        var fullName = RepositoryUrl.ParseFullName(repositoryUrl);

        var (enrollment, _) = user.GithubEnrollments.Find(fullName);

        return enrollment;
    }

    private void ThrowIfUserNotEnrolled(User user)
    {
        if (!user.GithubEnrollments.Any())
        {
            throw new ExecutionError("User is not enrolled to Github")
            {
                Code = "NOT_ENROLLED_TO_GITHUB"
            };
        }
    }
}
