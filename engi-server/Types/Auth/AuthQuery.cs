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
        var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        if (user == null)
        {
            return null;
        }

        var address = Address.Parse(user.Address);

        try
        {
            var info = await substrate.GetSystemAccountAsync(address);

            return new CurrentUserInfo(user, address, info);
        }
        catch (KeyNotFoundException)
        {
            return new CurrentUserInfo(user, address, null);
        }
    }
}
