namespace Engi.Substrate.Server;

public class LatestChainStateProvider
{
    private readonly IServiceProvider serviceProvider;

    public LatestChainStateProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task<ChainState> GetLatestChainState()
    {
        var observers = serviceProvider
            .GetServices<IChainObserver>()
            .ToArray();

        var chainSnapshotObserver = observers.OfType<ChainSnapshotObserver>().Single();
        var headObserver = observers.OfType<NewHeadChainObserver>().Single();

        return new()
        {
            Metadata = await chainSnapshotObserver.Metadata,
            Version = await chainSnapshotObserver.Version,
            GenesisHash = await chainSnapshotObserver.GenesisHash,
            LatestFinalizedHeader = headObserver.LastFinalizedHeader!
        };
    }
}