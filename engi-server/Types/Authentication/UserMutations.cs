using Engi.Substrate.Identity;
using Engi.Substrate.Keys;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types.Authentication;

public class UserMutations : ObjectGraphType
{
    public UserMutations()
    {
        this.AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<IdGraphType>("importKey")
            .Description(@"
                Import a user's key, to be managed by ENGI. The user is located with the key's address.
                If the key doesn't match a user's address on file, code NOT_FOUND is returned.
                If the key cannot be decoded, code INVALID_KEY is returned.  
                This mutation does not require authentication so it can be invoked directly after 
                registration (login would require e-mail confirmation).
            ")
            .Argument<NonNullGraphType<ImportUserKeyArgumentsGraphType>>("args")
            .ResolveAsync(ImportUserKeyAsync)
            .AllowAnonymous();
    }

    private async Task<object?> ImportUserKeyAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<ImportUserKeyArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        // make sure we can decode the key

        var userCrypto = scope.ServiceProvider.GetRequiredService<UserCryptographyService>();

        Keypair keypair;

        try
        {
            keypair = userCrypto.DecryptKeypair(args.EncryptedPkcs8Key);
        }
        catch (Exception)
        {
            throw new ExecutionError("Unable to decrypt and decode key.")
            {
                Code = "INVALID_KEY"
            };
        }

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var userAddressRef = await session
            .LoadAsync<UserAddressReference>(UserAddressReference.KeyFrom(keypair.Address),
                include => include.IncludeDocuments(x => x.UserId));

        if (userAddressRef == null)
        {
            throw new ExecutionError("User is not registered")
            {
                Code = "NOT_FOUND"
            };
        }

        var user = session.LoadAsync<User>(userAddressRef.UserId).Result;
        
        user.KeypairPkcs8 = userCrypto.EncryptKeypair(keypair);

        await session.SaveChangesAsync();

        return null;
    }
}