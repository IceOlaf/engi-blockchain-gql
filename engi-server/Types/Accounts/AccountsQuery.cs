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
            .ToDictionary(address => address, UserAddressReference.KeyFrom);

        var references = await session.LoadAsync<UserAddressReference>(referenceIdsByAddress.Values);

        return referenceIdsByAddress
            .Select((addressKeyValuePair) =>
            {
                var (address, id) = addressKeyValuePair;
                var result = references[id];

                return new AccountExistence
                {
                    Address = address,
                    Exists = references.TryGetValue(id, out var reference) && reference != null
                };
            });
    }
}
