using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class RootSubscriptions : ObjectGraphType
{
    public RootSubscriptions()
    {
        Field<HeaderGraphType>("newFinalizedHead")
            .Resolve(context => context.Source)
            .ResolveStream(context =>
            {
                var newHeadObserver = context.RequestServices!
                    .GetServices<IChainObserver>()
                    .OfType<NewHeadChainObserver>()
                    .Single();

                return newHeadObserver.FinalizedHeaders;
            });
    }
}
