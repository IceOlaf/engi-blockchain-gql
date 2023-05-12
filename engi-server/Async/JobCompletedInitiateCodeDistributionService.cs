using Engi.Substrate.Jobs;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions;
using Sentry;
using SessionOptions = Raven.Client.Documents.Session.SessionOptions;

namespace Engi.Substrate.Server.Async;

public class JobCompletedInitiateCodeDistributionService : SubscriptionProcessingBase<JobSnapshot>
{
    public JobCompletedInitiateCodeDistributionService(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        IHub sentry,
        IOptions<EngiOptions> engiOptions,
        ILoggerFactory loggerFactory)
        : base(store, serviceProvider, sentry, engiOptions, loggerFactory)
    {}

    protected override string CreateQuery()
    {
        return @"
from JobSnapshots where Solution != null
";
    }

    protected override async Task ProcessBatchAsync(SubscriptionBatch<JobSnapshot> batch, IServiceProvider serviceProvider)
    {
        // we react to job snapshots that have been complete by creating a distribute code command
        // this is done in a cluster-wide session to make sure that we only create one of those

        using var session = Store.OpenAsyncSession(new SessionOptions
        {
            TransactionMode = TransactionMode.ClusterWide
        });

        foreach (var item in batch.Items)
        {
            var command = new DistributeCodeCommand(item.Result);

            await session.StoreAsync(command);

            try
            {
                await session.SaveChangesAsync();
            }
            catch (ConcurrencyException)
            {
                // already exists, this is fine
            }

            session.Advanced.Clear();
        }
    }
}
