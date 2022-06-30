using GraphQL.Resolvers;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiSubscriptions : ObjectGraphType
{
    public EngiSubscriptions(IServiceProvider serviceProvider)
    {
        var newHeadObserver = serviceProvider.GetServices<IChainObserver>()
            .OfType<NewHeadChainObserver>()
            .Single();

        AddField(new EventStreamFieldType
        {
            Name = "newHead",
            Type = typeof(HeaderType),
            Resolver = new FuncFieldResolver<Header>(ctx => ctx.Source as Header),
            Subscriber = new EventStreamResolver<Header>(_ => newHeadObserver.Headers)
        });
    }
}