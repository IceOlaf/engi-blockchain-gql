using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiSubscriptions : ObjectGraphType
{
    public EngiSubscriptions(IServiceProvider serviceProvider)
    {
        var newHeadObserver = serviceProvider.GetServices<IChainObserver>()
            .OfType<NewHeadChainObserver>()
            .Single();

        Field<HeaderGraphType>("newFinalizedHead")
            .ResolveStream(_ => newHeadObserver.FinalizedHeaders);
    }
}