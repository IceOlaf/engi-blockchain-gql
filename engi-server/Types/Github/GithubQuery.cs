using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents.Session;
using User = Engi.Substrate.Identity.User;

namespace Engi.Substrate.Server;

public class GithubQuery : ObjectGraphType
{
    public GithubQuery()
    {
        this.AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<ListGraphType<GithubRepositoryGraphType>>("repositories")
            .Description("Get all repositories that the app installation gives us access to.")
            .ResolveAsync(GetRepositoriesAsync);
    }

    private async Task<object?> GetRepositoriesAsync(IResolveFieldContext<object?> context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        if (user.GithubEnrollments?.Any() != true)
        {
            throw new ExecutionError("User is not enrolled to Github")
            {
                Code = "NOT_ENROLLED_TO_GITHUB"
            };
        }

        return user.GithubEnrollments
            .Values
            .SelectMany(x => x.Repositories);
    }
}