using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text;
using Dasync.Collections;
using Engi.Substrate.Jobs;
using Engi.Substrate.Metadata.V14;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions;
using Raven.Client.Json;
using Sentry;

using Constants = Raven.Client.Constants;

namespace Engi.Substrate.Server.Indexing;

public class IndexingBackgroundService : SubscriptionProcessingBase<ExpandedBlock>
{
    private IDisposable? headerObservable;

    public IndexingBackgroundService(
        IDocumentStore store, 
        IServiceProvider serviceProvider,
        IHub sentry, 
        IWebHostEnvironment env,
        ILoggerFactory loggerFactory) 
        : base(store, serviceProvider, sentry, loggerFactory)
    {
        ProcessConcurrently = !env.IsDevelopment() && !env.IsEnvironment("CI");
    }

    protected override string CreateQuery()
    {
        return @"
            declare function filter(b) {
                return b.IndexedOn === null && b.SentryId === null && !c['@metadata'].hasOwnProperty('@refresh');
            }

            from ExpandedBlocks as b where filter(b) include PreviousId
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
                if (header.Number == 0)
                {
                    // this can only happen in a genesis, if that, but since it showed up once
                    // (after a genesis), I've added this guard - the indexing code will fail
                    // because block 0 doesn't have a timestamp set

                    return;
                }

                try
                {
                    using var session = Store.OpenAsyncSession();

                    session.Advanced.UseOptimisticConcurrency = true;

                    var currentBlock = new ExpandedBlock(header);

                    await session.StoreAsync(currentBlock);

                    try
                    {
                        await session.SaveChangesAsync();
                    }
                    catch (ConcurrencyException)
                    {
                        // we can ignore this, someone else stored it
                    }

                    // if we don't have a successful previous header,
                    // check whether the last block actually exists in the db 
                    // otherwise trigger an index

                    if (previousHeader == null && currentBlock.PreviousId != null)
                    {
                        // fire and forget
#pragma warning disable CS4014
                        EnsureIndexingConsistencyAsync(header.Number - 1);
#pragma warning restore CS4014
                    }

                    previousHeader = header;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to index new header={number}", header.Number);

                    // in the case of an error saving, don't hold up the indexing but
                    // reset the previousHeader variable so that the next block will
                    // index the missed ones

                    previousHeader = null;
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
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var snapshotObserver = serviceProvider
            .GetServices<IChainObserver>()
            .OfType<ChainSnapshotObserver>()
            .Single();

        var meta = await snapshotObserver.Metadata;

        using var session = batch.OpenAsyncSession();

        // account for jobs stored

        session.Advanced.MaxNumberOfRequestsPerSession = batch.NumberOfItemsInBatch * 10; 

        var previousBlocks = await session
            .LoadAsync<ExpandedBlock>(batch.Items.Select(x => x.Result.PreviousId));

        var resultBag = new ConcurrentBag<object>();

        var metadataById = batch.Items
            .ToDictionary(x => x.Id, x => session.Advanced.GetMetadataFor(x.Result));

        await batch.Items.ParallelForEachAsync(async doc =>
        {
            var client = new SubstrateClient(httpClientFactory);

            var block = doc.Result;
            var previous = block.PreviousId != null ? previousBlocks[block.PreviousId] : null;

            void ConfigureScope(Scope scope)
            {
                scope.SetExtras(new Dictionary<string, object?>
                {
                    ["number"] = block.Number.ToString(),
                    ["hash"] = block.Hash ?? string.Empty
                });
            }

            try
            {
                var results = await ProcessBatchItemAsync(block, previous, meta, client);

                foreach (var result in results)
                {
                    resultBag.Add(result);
                }

                block.IndexedOn = DateTime.UtcNow;
            }
            catch (BlockHeaderNotFoundException ex)
            {
                // this shouldn't happen so until we figure it out, keep posting the error to sentry

                Sentry.CaptureException(ex, ConfigureScope);

                // then reschedule a try in 2 seconds

                var metadata = metadataById[doc.Id];

                metadata[Constants.Documents.Metadata.Refresh] = DateTime.UtcNow.AddSeconds(2);
            }
            catch (Exception ex) when (ex is TimeoutException or TaskCanceledException { InnerException: TimeoutException })
            {
                // then reschedule a try immediately

                var metadata = metadataById[doc.Id];

                metadata[Constants.Documents.Metadata.Refresh] = DateTime.UtcNow.AddSeconds(0);
            }
            catch (Exception ex)
            {
                // logged as debug so it's not picked by Sentry twice - we need to sentry id
                // so we must rely on the native call

                Logger.LogDebug(ex, "Indexing failed; block number={number}", block.Number);

                // if we didn't make it to the end, store the sentry error

                block.SentryId = Sentry.CaptureException(ex, ConfigureScope).ToString();
            }
        }, maxDegreeOfParallelism: 64);

        foreach (var result in resultBag)
        {
            await session.StoreAsync(result, null, session.Advanced.GetDocumentId(result));
        }

        await session.SaveChangesAsync();
    }

    private async Task<IEnumerable<object>> ProcessBatchItemAsync(
        ExpandedBlock block,
        ExpandedBlock? previous,
        RuntimeMetadata meta,
        SubstrateClient client)
    {
        var results = new List<object>();

        string hash = block.Hash ?? await client.GetChainBlockHashAsync(block.Number);

        var signedBlock = await client.GetChainBlockAsync(hash);

        var events = await client.GetSystemEventsAsync(hash, meta);

        block.Fill(signedBlock!.Block, events, meta);
        
        // TODO: make this query in one storage call if possible?

        foreach (var indexable in GetIndexables(block))
        {
            if (indexable is JobIndexable jobIndexable)
            {
                var snapshot = await RetrieveJobSnapshotAsync(jobIndexable.JobId, block, client);

                snapshot.IsCreation = jobIndexable.IsCreation;

                results.Add(snapshot);
            }
            else if (indexable is AttemptIndexable attemptIndexable)
            {
                results.Add(JobAttemptedSnapshot.From(attemptIndexable.Data, block));
            }
            else if (indexable is SolutionIndexable solutionIndexable)
            {
                // some solutions have been updated, fetch and store snapshots of those
                // that belong to the solution from this solve_job invocation

                var storageKeys = solutionIndexable.TestIds
                    .Select(testId => StorageKeys.Jobs.ForTestSolution(solutionIndexable.JobId, testId))
                    .ToArray();

                var solutions = await client.QueryStorageAtAsync(storageKeys, Solution.Parse, block.Hash);

                foreach (var solution in solutions.Values
                    .Where(x => x != null && x.SolutionId == solutionIndexable.SolutionId)
                    // solutions are copies to each test so can be dupes
                    .DistinctBy(x => x!.SolutionId))
                {
                    var solutionSnapshot = new SolutionSnapshot(solution!, block);

                    results.Add(solutionSnapshot);
                }
            }
        }

        // make sure the previous block exists, as a sanity check and if not
        // trigger its indexing

        if (previous == null && block.PreviousId != null)
        {
            results.Add(
                new ExpandedBlock(block.Number - 1, block.ParentHash));
        }

        return results;
    }

    private async Task<JobSnapshot> RetrieveJobSnapshotAsync(
        ulong jobId,
        ExpandedBlock block,
        SubstrateClient client)
    {
        string snapshotStorageKey = StorageKeys.Jobs.ForJobId(jobId);

        return (await client.GetStateStorageAsync(snapshotStorageKey,
            reader => JobSnapshot.Parse(reader, block), block.Hash!))!;
    }

    private IEnumerable<Indexable> GetIndexables(ExpandedBlock block)
    {
        foreach (var extrinsic in block.Extrinsics.Where(x => x.IsSuccessful))
        {
            if (extrinsic.PalletName == ChainKeys.Jobs.Name)
            {
                if (extrinsic.CallName == "create_job")
                {
                    var jobIdGeneratedEvent = extrinsic.Events
                        .Find(ChainKeys.Jobs.Name, ChainKeys.Jobs.Events.JobIdGenerated);

                    yield return new JobIndexable
                    {
                        JobId = (ulong)jobIdGeneratedEvent.Data,
                        IsCreation = true
                    };
                }
                else if (extrinsic.CallName == "attempt_job")
                {
                    var jobAttemptedEvent = extrinsic.Events
                        .Find(ChainKeys.Jobs.Name, ChainKeys.Jobs.Events.JobAttempted);

                    var data = (Dictionary<int, object>) jobAttemptedEvent.Data;

                    yield return new AttemptIndexable
                    {
                        Data = data
                    };
                }

                continue;
            }

            if (extrinsic.PalletName == "Sudo" && extrinsic.ArgumentKeys.Contains("call"))
            {
                var call = extrinsic.Arguments["call"] as Dictionary<string, object>;

                if (call?.ContainsKey("Jobs") == true)
                {
                    var jobs = (Dictionary<string, object>) call["Jobs"];

                    if (jobs.ContainsKey("solve_job"))
                    {
                        var solveJob = (Dictionary<string, object>) jobs["solve_job"];
                        var attempt = (Dictionary<string, object>) solveJob["attempt"];
                        var tests = (object[]) attempt["tests"];

                        yield return new SolutionIndexable
                        {
                            JobId = (ulong) solveJob["job"],
                            SolutionId = (ulong) solveJob["id"],
                            TestIds = tests
                                .Cast<Dictionary<string, object>>()
                                .Select(test => Encoding.UTF8.GetString(Hex.GetBytes0X((string) test["id"])))
                                .ToArray()
                        };
                    }
                }

                continue;
            }
        }
    }

    private async Task EnsureIndexingConsistencyAsync(ulong toInclusive)
    {
        try
        {
            const int walkSize = 256;

            for (ulong number = 1; number <= toInclusive; number += walkSize)
            {
                await EnsureIndexingConsistencyAsync(number,
                    number + walkSize <= toInclusive ? number + walkSize : toInclusive);
            }
        }
        catch (Exception ex)
        {
            Sentry.CaptureException(ex);
        }
    }

    private async Task EnsureIndexingConsistencyAsync(ulong fromInclusive, ulong toInclusive)
    {
        var indexes = Enumerable.Range(0, (int) (toInclusive - fromInclusive))
            .Select(offset => fromInclusive + (ulong) offset)
            .ToArray();

        if (!indexes.Any())
        {
            return;
        }

        string[] keys = indexes
            .Select(ExpandedBlock.KeyFrom)
            .ToArray();

        using var session = Store.OpenAsyncSession();

        var loadedBlocks = await session.LoadAsync<ExpandedBlock>(keys);

        var missingKeys = loadedBlocks
            .Where(x => x.Value == null)
            .Select(x => x.Key)
            .OrderByDescending(x => x)
            .ToArray();

        if (!missingKeys.Any())
        {
            return;
        }

        foreach (var key in missingKeys)
        {
            ulong number = ulong.Parse(key.Split('/').Last());

            if (number == 0)
            {
                // this would indicate a bug in the parent code but since it's a cheap operation
                // i've added this to prevent problems with indexing block 0 (lacks timestamp)

                continue;
            }

            var block = new ExpandedBlock(number);

            var documentInfo = new DocumentInfo
            {
                MetadataInstance = new MetadataAsDictionary
                {
                    { Constants.Documents.Metadata.Collection, Store.Conventions.GetCollectionName(typeof(ExpandedBlock)) },
                    { Constants.Documents.Metadata.RavenClrType, Store.Conventions.FindClrTypeName(typeof(ExpandedBlock)) }
                }
            };

            session.Advanced.Defer(new PatchCommandData(block.Id, changeVector: null,
                // intentionally left blank - we only want to modify it if it doesn't exist
                // essentially doing an insert-if-doesn't-exist without concurrency concerns
                new PatchRequest { Script = string.Empty }, 
                new PatchRequest
                {
                    Script =
    @"
        for(var key in args.Block) {
            this[key] = args.Block[key];
        }
    ",
                    Values = new Dictionary<string, object>
                    {
                        { "Block", session.Advanced.JsonConverter.ToBlittable(block, documentInfo) }
                    }
                }));
        }

        await session.SaveChangesAsync();
    }

    interface Indexable { }

    class JobIndexable : Indexable
    {
        public ulong JobId { get; init; }

        public bool IsCreation { get; init; }
    }

    class AttemptIndexable : Indexable
    {
        public Dictionary<int, object> Data { get; init; } = null!;
    }

    class SolutionIndexable : Indexable
    {
        public ulong JobId { get; init; }

        public ulong SolutionId { get; init; }

        public string[] TestIds { get; init; } = null!;
    }
}