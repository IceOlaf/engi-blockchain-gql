using System.Reactive.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Sentry;

namespace Engi.Substrate.Server.Indexing;

public class IndexingBackgroundService : SubscriptionProcessingBase<ExpandedBlock>
{
    private IDisposable? headerObservable;

    public IndexingBackgroundService(
        IDocumentStore store, 
        IServiceProvider serviceProvider,
        IHub sentry, 
        ILoggerFactory loggerFactory) 
        : base(store, serviceProvider, sentry, loggerFactory)
    {
    }

    protected override string CreateQuery()
    {
        return @"
            declare function filter(b) {
                return b.IndexedOn === null && !b['@metadata'].hasOwnProperty('SentryId')
            }

            from ExpandedBlocks as b where filter(b)
        ";
    }

    protected override Task InitializeAsync()
    {
        var headObserver = ServiceProvider
            .GetServices<IChainObserver>()
            .OfType<NewHeadChainObserver>()
            .Single();

        Header? previousHeader = null;

        headerObservable = headObserver.FinalizedHeaders
            // this Rx sequence makes sure that each handler is awaited before continuing
            .Select(header => Observable.FromAsync(async () => 
            {
                try
                {
                    // index all the blocks from the last known one to the current

                    ulong lastIndexedBlockNumber = previousHeader?.Number
                        ?? await FindLastIndexedBlockNumberAsync(header.Number);

                    // check whether the last block actually exists in the db - this is an easy
                    // way to allow re-indexes to occur by deleting all previous documents

                    if (lastIndexedBlockNumber != 0)
                    {
                        using var session = Store.OpenAsyncSession();

                        var lastIndexedBlock = await session
                            .LoadAsync<ExpandedBlock>(ExpandedBlock.KeyFrom(lastIndexedBlockNumber));

                        if (lastIndexedBlock == null)
                        {
                            lastIndexedBlockNumber = 0;
                        }
                    }

                    await TriggerIndexingForBlockNumbers(lastIndexedBlockNumber, header.Number);

                    previousHeader = header;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to index new header={number}", header.Number);
                }
            }))
            .Concat()
            .Subscribe();

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        base.Dispose();

        headerObservable?.Dispose();
    }

    protected override async Task ProcessBatchAsync(
        SubscriptionBatch<ExpandedBlock> batch, IServiceProvider serviceProvider)
    {
        using var session = batch.OpenAsyncSession();

        var client = serviceProvider.GetRequiredService<SubstrateClient>();
        var snapshotObserver = serviceProvider
            .GetServices<IChainObserver>()
            .OfType<ChainSnapshotObserver>()
            .Single();

        var meta = snapshotObserver.Metadata;

        foreach (var doc in batch.Items)
        {
            var job = doc.Result;

            Sentry.AddBreadcrumb("Processing block",
                data: new Dictionary<string, string>
                {
                    ["number"] = job.Number.ToString()
                });

            try
            {
                string hash = await client.GetChainBlockHashAsync(job.Number);

                var signedBlock = await client.GetChainBlockAsync(hash);

                var events = await client.GetSystemEventsAsync(hash, meta);

                job.Fill(signedBlock!.Block, events, meta);
            }
            catch (Exception ex)
            {
                // logged as debug so it's not picked by Sentry twice - we need to sentry id
                // so we must rely on the native call

                Logger.LogDebug(ex, "Indexing failed; block number={number}", job.Number);

                // if we didn't make it to the end, store the sentry error

                var jobMeta = session.Advanced.GetMetadataFor(job);

                jobMeta["SentryId"] = Sentry.CaptureException(ex).ToString();
            }
        }

        await session.SaveChangesAsync();
    }

    private async Task TriggerIndexingForBlockNumbers(ulong fromExclusive, ulong toInclusive)
    {
        await using var bulk = Store.BulkInsert();

        for (ulong number = fromExclusive + 1; number <= toInclusive; ++number)
        {
            await bulk.StoreAsync(new ExpandedBlock(number));
        }
    }

    private async Task<ulong> FindLastIndexedBlockNumberAsync(ulong lastBlockNumber)
    {
        const int walkSize = 256;

        using var session = Store.OpenAsyncSession();

        session.Advanced.MaxNumberOfRequestsPerSession = int.MaxValue;

        // walk back from the current block number to find the last indexed block

        while (true)
        {
            session.Advanced.Clear();
            
            var indexes = Enumerable.Range(0, walkSize)
                .Select(offset =>
                {
                    try
                    {
                        return checked(lastBlockNumber - (uint)offset);
                    }
                    catch (OverflowException)
                    {
                        return 0ul;
                    }
                })
                .ToList();

            if (indexes.Last() == 0)
            {
                indexes.RemoveAll(index => index == 0);
            }

            if (!indexes.Any())
            {
                lastBlockNumber = 0;

                break;
            }

            string[] keys = indexes
                .Select(ExpandedBlock.KeyFrom)
                .ToArray();

            var loadedBlocks = await session.LoadAsync<ExpandedBlock>(keys);

            var missingKeys = loadedBlocks
                .Where(x => x.Value == null)
                .Select(x => x.Key)
                .OrderByDescending(x => x)
                .ToArray();

            if (!missingKeys.Any())
            {
                return lastBlockNumber;
            }

            if (missingKeys.Length == keys.Length)
            {
                // nothing was found, skip to the next batch

                lastBlockNumber = indexes.Last() - 1;

                if (keys.Length < walkSize)
                {
                    break;
                }

                continue;
            }

            // last known block is bottom of stack, minus one

            string topKey = missingKeys.Last();

            lastBlockNumber = ulong.Parse(topKey.Split('/').Last()) - 1;
        }

        return lastBlockNumber;
    }
}