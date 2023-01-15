using Engi.Substrate.Identity;
using Engi.Substrate.Keys;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using SessionOptions = Raven.Client.Documents.Session.SessionOptions;

namespace Engi.Substrate.Server.Types.Authentication;

public class UserMutations : ObjectGraphType
{
    public UserMutations()
    {
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

        Field<CurrentUserInfoGraphType>("update")
            .Description(@"
                Update current user.
            ")
            .Argument<NonNullGraphType<UpdateUserArgumentsGraphType>>("args")
            .ResolveAsync(UpdateUserAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);
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

    private async Task<object?> UpdateUserAsync(IResolveFieldContext<object?> context)
    {
        var args = context.GetValidatedArgument<UpdateUserArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IDocumentStore>()
            .OpenAsyncSession(new SessionOptions
            {
                TransactionMode = TransactionMode.ClusterWide
            });

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        if (!string.IsNullOrEmpty(args.Email)
            && !string.Equals(args.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            string emailReferenceKey = UserEmailReference.KeyFrom(user.Email);

            session.Delete(emailReferenceKey);

            // update

            user.Email = args.Email.ToLowerInvariant();

            await session.StoreAsync(new UserEmailReference(user));
        }

        if (!string.IsNullOrEmpty(args.Display))
        {
            user.Display = args.Display;
        }

        if (args.FreelancerSettings != null)
        {
            user.FreelancerSettings = args.FreelancerSettings;
        }

        if (args.BusinessSettings != null)
        {
            user.BusinessSettings = args.BusinessSettings;
        }

        if (args.EmailSettings != null)
        {
            user.EmailSettings = args.EmailSettings;
        }

        try
        {
            await session.SaveChangesAsync();
        }
        catch (ConcurrencyException)
        {
            throw new ExecutionError("E-mail conflict.")
            {
                Code = "EMAIL_CONFLICT"
            };
        }

        return (CurrentUserInfo) user;
    }
}
