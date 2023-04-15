using Engi.Substrate.Identity;
using Engi.Substrate.Server.Types.Validation;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types;

public class AccountsQuery : ObjectGraphType
{
    public AccountsQuery()
    {
        Field<ListGraphType<AccountExistenceGraphType>>("existence")
            .Description("Query for the account existence of one or more addresses.")
            .Argument<NonNullGraphType<ListGraphType<StringGraphType>>>("addresses")
            .ResolveAsync(CheckExistenceAsync)
            .AllowAnonymous();
    }

    private async Task<object?> CheckExistenceAsync(IResolveFieldContext<object?> context)
    {
        var addresses = context.GetArgument<string[]>("addresses");

        if (addresses.Length is 0 or > 10)
        {
            throw new ArgumentValidationException(nameof(addresses),
                "Must provide at least one and less than 10 addresses.");
        }

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var referenceIdsByAddress = addresses
            .ToDictionary(address => address, address =>
            {
                try
                {
                    var parsedAddress = Address.Parse(address);

                    return UserAddressReference.KeyFrom(parsedAddress);
                }
                catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "address")
                {
                    // failed to parse, address is not supported

                    return null;
                }
            });

        var validAddresses = referenceIdsByAddress.Values
            .Where(value => value != null)
            .ToArray();

        var references = await session
            .LoadAsync<UserAddressReference>(validAddresses,
                include => include.IncludeDocuments(x => x.UserId));

        var usersById = await session
            .LoadAsync<User>(references.Values
                .Where(x => x != null)
                .Select(x => x.UserId));

        return referenceIdsByAddress
            .Select(addressKeyValuePair =>
            {
                var (address, id) = addressKeyValuePair;

                User? user = null;

                if (id != null
                    && references.TryGetValue(id, out var reference)
                    && reference != null)
                {
                    user = usersById[reference.UserId];
                }

                return new AccountExistence
                {
                    Address = address,
                    Exists = GetResult(id, user)
                };
            });
    }

    private static AccountExistenceResult GetResult(string? id, User? user)
    {
        if (id == null)
        {
            return AccountExistenceResult.UnsupportedAddress;
        }

        if (user == null)
        {
            return AccountExistenceResult.No;
        }

        return user.EmailConfirmedOn.HasValue ? AccountExistenceResult.Yes : AccountExistenceResult.Unconfirmed;
    }
}
