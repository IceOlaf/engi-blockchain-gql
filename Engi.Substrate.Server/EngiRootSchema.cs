using Engi.Substrate.Pallets;
using Engi.Substrate.Server.Types;
using GraphQL.Types;

namespace Engi.Substrate.Server;

public class EngiRootSchema : Schema
{
    public EngiRootSchema(
        IServiceProvider serviceProvider)
    {
        RegisterTypeMapping(typeof(AccountData), typeof(AccountDataGraphType));
        RegisterTypeMapping(typeof(Digest), typeof(DigestType));
        RegisterTypeMapping(typeof(AccountMetadata), typeof(AccountMetadataType));

        Query = new EngiQuery(serviceProvider);
        Mutation = new EngiMutations(serviceProvider);
        Subscription = new EngiSubscriptions(serviceProvider);
    }
}