using Engi.Substrate.Identity;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types;

public class AuthQuery : ObjectGraphType
{
    public AuthQuery()
    {
        this.AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<CurrentUserInfoGraphType>("currentUser")
            .Description("Get information about the current user.")
            .ResolveAsync(GetCurrentUserInfoAsync);
    }

    private async Task<object?> GetCurrentUserInfoAsync(IResolveFieldContext<object?> context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        return (CurrentUserInfo) user;
    }
}