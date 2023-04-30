using Engi.Substrate.Server.Indexing;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions;
using Raven.Client.Json;
using Sentry;
using Constants = Raven.Client.Constants;

namespace Engi.Substrate.Server.Async;

public class ConsistencyCheckService : SubscriptionProcessingBase<ConsistencyCheckCommand>
{
    public ConsistencyCheckService(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        IHub sentry,
        IOptions<EngiOptions> engiOptions,
        ILoggerFactory loggerFactory)
        : base(store, serviceProvider, sentry, engiOptions, loggerFactory)
    {
        ProcessConcurrently = false;
    }

    protected override string CreateQuery()
    {
        return @"
declare function filter(c) {
    return !c['@metadata'].hasOwnProperty('@refresh');
}

from ConsistencyCheckCommands as c where filter(c) 
";
    }

    protected override async Task InitializeAsync()
    {
        using var session = Store.OpenAsyncSession();

        // if someone else is working on the document, leave it alone

        session.Advanced.UseOptimisticConcurrency = true;

        var command = await session
            .LoadAsync<ConsistencyCheckCommand>(
                ConsistencyCheckCommand.Key);

        if (command == null)
        {
            command = new ConsistencyCheckCommand();

            await session.StoreAsync(command);
        }
        else
        {
            var metadata = session.Advanced.GetMetadataFor(command);

            // make sure there is a refresh value

            if (!metadata.ContainsKey(Constants.Documents.Metadata.Refresh))
            {
                metadata[Constants.Documents.Metadata.Refresh] = DateTime.UtcNow.AddMinutes(15);
            }
        }

        try
        {
            await session.SaveChangesAsync();
        }
        catch (ConcurrencyException)
        {
            // ignore
        }
    }

    protected override async Task ProcessBatchAsync(SubscriptionBatch<ConsistencyCheckCommand> batch, IServiceProvider serviceProvider)
    {
        using var session = batch.OpenAsyncSession();

        // if the consistency check is running but another process modifies the document
        // we want to allow the service to replace any changes (most likely made by nodes
        // executing InitializeAsync)

        session.Advanced.UseOptimisticConcurrency = false;

        foreach (var item in batch.Items)
        {
            var command = item.Result;

            var headerObserver = serviceProvider.GetServices<IChainObserver>()
                .OfType<NewHeadChainObserver>()
                .Single();

            if (headerObserver.LastFinalizedHeader != null)
            {
                command.LastRecovered = await EnsureIndexingConsistencyAsync(headerObserver.LastFinalizedHeader.Number);

                command.LastExecutedOn = DateTime.UtcNow;
            }

            var meta = session.Advanced.GetMetadataFor(command);

            meta[Constants.Documents.Metadata.Refresh] = DateTime.UtcNow.AddMinutes(15);
        }

        await session.SaveChangesAsync();
    }

    private async Task<long> EnsureIndexingConsistencyAsync(ulong toInclusive)
    {
        long recovered = 0;

        try
        {
            const int walkSize = 256;

            for (ulong number = 1; number <= toInclusive; number += walkSize)
            {
                recovered += await EnsureIndexingConsistencyAsync(number,
                    number + walkSize <= toInclusive ? number + walkSize : toInclusive);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed while executing consistency check.");
        }

        return recovered;
    }

    private async Task<long> EnsureIndexingConsistencyAsync(ulong fromInclusive, ulong toInclusive)
    {
        var indexes = Enumerable.Range(0, (int)(toInclusive - fromInclusive))
            .Select(offset => fromInclusive + (ulong)offset)
            .ToArray();

        if (!indexes.Any())
        {
            return 0;
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
            return 0;
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

        return missingKeys.Length;
    }
}
