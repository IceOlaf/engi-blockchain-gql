using Engi.Substrate.Server.Types;
using GraphQL.Types;

namespace Engi.Substrate.Server;

public class RootSchema : Schema
{
    public RootSchema()
    {
        Query = new EngiQuery();
        Mutation = new RootMutations();
        Subscription = new EngiSubscriptions();
    }
}