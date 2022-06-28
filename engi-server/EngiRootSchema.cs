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
        RegisterTypeMapping(typeof(AccountMetadata), typeof(AccountMetadataGraphType));
        RegisterTypeMapping(typeof(Digest), typeof(DigestGraphType));
        RegisterTypeMapping(typeof(GenericEvent), typeof(GenericEventGraphType));
        RegisterTypeMapping(typeof(Phase), typeof(PhaseGraphType));

        Query = new EngiQuery(serviceProvider);
        Mutation = new EngiMutations(serviceProvider);
        Subscription = new EngiSubscriptions(serviceProvider);
    }
}