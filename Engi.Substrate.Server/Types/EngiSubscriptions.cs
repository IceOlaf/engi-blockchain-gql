using GraphQL.Resolvers;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiSubscriptions : ObjectGraphType
{
    public EngiSubscriptions(IObservable<Header> stream)
    {
        AddField(new EventStreamFieldType
        {
            Name = "newHead",
            Type = typeof(HeaderType),
            Resolver = new FuncFieldResolver<Header>(ctx => ctx.Source as Header),
            Subscriber = new EventStreamResolver<Header>(_ => stream)
        });
    }
}