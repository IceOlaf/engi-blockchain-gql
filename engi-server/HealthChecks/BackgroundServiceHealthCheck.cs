using System.Collections.ObjectModel;
using Engi.Substrate.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Engi.Substrate.Server.HealthChecks;

public class BackgroundServiceHealthCheck<T> : IHealthCheck
    where T : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    public BackgroundServiceHealthCheck(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var backgroundService = serviceProvider.GetServices<IHostedService>()
            .OfType<T>()
            .SingleOrDefault();

        if (backgroundService == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Background service is not registered."));
        }

        var data = backgroundService is IHasHealthCheckData hasHealthCheckData
            ? hasHealthCheckData.GetHealthCheckData()
            : new Dictionary<string, object?>();

        return CheckHealthAsync(backgroundService, data!, cancellationToken);
    }

    protected virtual Task<HealthCheckResult> CheckHealthAsync(
        T service,
        IReadOnlyDictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        if (service.ExecuteTask.IsCompleted)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Background service is not running.", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(data: data));
    }
}
