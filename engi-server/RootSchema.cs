using Engi.Substrate.Server.Types;
using Engi.Substrate.Server.Types.Authentication;
using Engi.Substrate.Server.Types.Validation;
using GraphQL.Instrumentation;
using GraphQL.Types;

namespace Engi.Substrate.Server;

public class RootSchema : Schema
{
    public RootSchema()
    {
        Query = new RootQuery();
        Mutation = new RootMutations();
        Subscription = new EngiSubscriptions();

        FieldMiddleware.Use(new NoMultipleAuthMutationsMiddleware());
        FieldMiddleware.Use(new ValidationMiddleware());
    }
}
