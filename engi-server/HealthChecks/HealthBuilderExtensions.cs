using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Engi.Substrate.Server.HealthChecks;

public static class HealthBuilderExtensions
{
    public static IHealthChecksBuilder AddBackgroundServiceHealthCheck<TService>(this IHealthChecksBuilder builder, HealthStatus? failureStatus = null)
        where TService : BackgroundService
    {
        return builder.AddCheck<BackgroundServiceHealthCheck<TService>>(
            typeof(TService).Name.ToKebabCase(), failureStatus);
    }

    public static IHealthChecksBuilder AddRavenSubscriptionHealthCheck<TSubscription, TDocument>(this IHealthChecksBuilder builder, HealthStatus? failureStatus = null)
        where TSubscription : SubscriptionProcessingBase<TDocument>
        where TDocument : class
    {
        return builder.AddCheck<RavenSubscriptionServiceHealthCheck<TSubscription, TDocument>>(
            typeof(TSubscription).Name.ToKebabCase(), failureStatus);
    }
}
