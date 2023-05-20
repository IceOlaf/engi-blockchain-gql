using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Documents;
using Raven.Client.Exceptions.Documents.Subscriptions;

namespace Engi.Substrate.Server.HealthChecks;

public class RavenSubscriptionServiceHealthCheck<TSubscriptionService, T> : BackgroundServiceHealthCheck<TSubscriptionService>
    where TSubscriptionService : SubscriptionProcessingBase<T>
    where T : class
{
    private readonly IDocumentStore store;

    public RavenSubscriptionServiceHealthCheck(IDocumentStore store, IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        this.store = store;
    }

    protected override async Task<HealthCheckResult> CheckHealthAsync(TSubscriptionService service,
        IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken)
    {
        var serviceResult = await base.CheckHealthAsync(service, data, cancellationToken);

        if (serviceResult.Status == HealthStatus.Healthy)
        {
            return serviceResult;
        }

        string subscriptionName = SubscriptionConventions.GetName(typeof(TSubscriptionService));

        SubscriptionState state;

        try
        {
            state = await store.Subscriptions
                .GetSubscriptionStateAsync(subscriptionName, token: cancellationToken);
        }
        catch (SubscriptionDoesNotExistException)
        {
            return HealthCheckResult.Unhealthy("Subscription does not exist.", data: data);
        }

        if (state.Disabled)
        {
            return HealthCheckResult.Degraded("Subscription is disabled.", data: data);
        }

        return HealthCheckResult.Healthy(data: data);
    }
}
