using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiSubscriptions : ObjectGraphType
{
    public EngiSubscriptions()
    {
        Field<HeaderGraphType>("newFinalizedHead")
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