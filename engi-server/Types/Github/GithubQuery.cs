using Engi.Substrate.Github;
using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;
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

        Field<UserGithubEnrollmentGraphType>("jobAuthorization")
            .Description("Get GitHub authorization for a particular job.")
            .Argument<NonNullGraphType<StringGraphType>>("jobId")
            .ResolveAsync(GetAuthorizationForJobAsync)
            .AuthorizeWithPolicy(PolicyNames.Sudo);
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

        var octokitFactory = scope.ServiceProvider.GetRequiredService<GithubClientFactory>();

        IReadOnlyList<GitHubCommit>? commits = null;

        if (user.GithubEnrollments.Any())
        {
            var (matchingEnrollment, matchingRepo) = user.GithubEnrollments.Find(repoOwner, repoName);

            if (matchingEnrollment != null)
            {
                var octokit = await octokitFactory.CreateForAsync(matchingEnrollment.InstallationId);

                commits = await octokit.Repository.Commit.GetAll(matchingRepo!.Id, new CommitRequest { Sha = branch });
            }
        }

        if (commits == null)
        {
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

    private async Task<object?> GetAuthorizationForJobAsync(IResolveFieldContext<object?> context)
    {
        ulong jobId = context.GetArgument<ulong>("jobId");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var reference = await session
            .LoadAsync<ReduceOutputReference>(JobIndex.ReferenceKeyFrom(jobId),
                include => include.IncludeDocuments<ReduceOutputReference>(x => x.ReduceOutputs));

        if (reference == null)
        {
            return null;
        }

        var job = await session.LoadAsync<Job>(reference.ReduceOutputs.First());

        if(job == null)
        {
            throw new ExecutionError("Job was not found.")
            {
                Code = "NOT_FOUND"
            };
        }

        var creatorReference = await session
            .LoadAsync<UserAddressReference>(UserAddressReference.KeyFrom(job.Creator),
                include => include.IncludeDocuments(x => x.UserId));

        if(creatorReference == null)
        {
            throw new ExecutionError("Creator account was not found.")
            {
                Code = "NOT_FOUND"
            };
        }

        var creator = await session.LoadAsync<User>(creatorReference.UserId);

        var (enrollment, _) = creator.GithubEnrollments.Find(job.Repository.FullName);

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
