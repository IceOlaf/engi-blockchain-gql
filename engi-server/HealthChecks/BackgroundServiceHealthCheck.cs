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

    public virtual Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var backgroundService = serviceProvider.GetServices<IHostedService>()
            .OfType<BackgroundService>()
            .SingleOrDefault(x => x.GetType() == typeof(T));

        if (backgroundService == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Background service is not registered."));
        }

        if (backgroundService.ExecuteTask.IsCompleted)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Background service is not running."));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
