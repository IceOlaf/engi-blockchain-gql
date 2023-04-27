using Engi.Substrate.Jobs;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions;
using Sentry;

using SessionOptions = Raven.Client.Documents.Session.SessionOptions;

namespace Engi.Substrate.Server.Async;

public class JobAttemptQueueingService : SubscriptionProcessingBase<JobAttemptedSnapshot>
{
    public JobAttemptQueueingService(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        IWebHostEnvironment env,
        IHub sentry,
        ILoggerFactory loggerFactory)
        : base(store, serviceProvider, env, sentry, loggerFactory)
    {}

    protected override string CreateQuery()
    {
        return @"from JobAttemptedSnapshots where DispatchedOn = null";
    }

    protected override async Task ProcessBatchAsync(
        SubscriptionBatch<JobAttemptedSnapshot> batch,
        IServiceProvider serviceProvider)
    {
        // use a cluster session here to prevent dupes if an attempt is re-indexed
        Logger.LogInformation("Processing attempt batches");

        using var session = Store.OpenAsyncSession(new SessionOptions
        {
            TransactionMode = TransactionMode.ClusterWide
        });

        foreach(var item in batch.Items)
        {
            var attempt = item.Result;

            var command = new QueueEngineRequestCommand
            {
                Id = $"QueueEngineRequestCommand/for/{attempt.AttemptId}",
                Identifier = attempt.Id,
                CommandString = $"job attempt {attempt.PatchFileUrl} --job-id {attempt.JobId} --dry-run",
                SourceId = attempt.Id
            };

            await session.StoreAsync(command);

            try
            {
                await session.SaveChangesAsync();
            }
            catch(ConcurrencyException)
            {
                // this has been queued before it seems, clear the sesion so we can process the rest

                session.Advanced.Clear();
            }
        }
        Logger.LogInformation("Done processing attempt batches");
    }
}
