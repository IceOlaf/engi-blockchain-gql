using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Documents;

namespace Engi.Substrate.Server.HealthChecks;

public class RavenIndexHealthCheck<TIndex> : IHealthCheck
    where TIndex : AbstractIndexCreationTask
{
    private readonly IDocumentStore store;

    public RavenIndexHealthCheck(IDocumentStore store)
    {
        this.store = store;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        string indexName = typeof(TIndex).Name;

        var definition = await store.Maintenance
            .SendAsync(new GetIndexOperation(indexName), cancellationToken);

        if (definition == null)
        {
            return HealthCheckResult.Unhealthy("Index was not found.");
        }

        // check stats and errors

        var indexStats = await store.Maintenance
            .SendAsync(new GetIndexStatisticsOperation(indexName), cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["state"] = indexStats.State,
            ["isStale"] = indexStats.IsStale,
            ["errorsCount"] = indexStats.ErrorsCount,
            ["lastIndexingTime"] = indexStats.LastIndexingTime!,
            ["lastQueryingTime"] = indexStats.LastQueryingTime!
        };

        if (indexStats.ErrorsCount <= 0)
        {
            return HealthCheckResult.Healthy(data: data);
        }

        var indexErrors = await store.Maintenance
            .SendAsync(new GetIndexErrorsOperation(new[] { indexName }), cancellationToken);

        data["errors"] = indexErrors.First().Errors;

        return HealthCheckResult.Degraded("Index has errors.", data: data);
    }
}
