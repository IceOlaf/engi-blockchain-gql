using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Documents;
using Raven.Client.Exceptions.Documents.Subscriptions;

namespace Engi.Substrate.Server.HealthChecks;

public class RavenSubscriptionServiceHealthCheck<TSubscription, T> : BackgroundServiceHealthCheck<TSubscription>
    where TSubscription : SubscriptionProcessingBase<T>
    where T : class
{
    private readonly IDocumentStore store;

    public RavenSubscriptionServiceHealthCheck(IDocumentStore store, IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        this.store = store;
    }

    public override async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var serviceResult = await base.CheckHealthAsync(context, cancellationToken);

        if (serviceResult.Status == HealthStatus.Healthy)
        {
            return serviceResult;
        }

        string subscriptionName = SubscriptionConventions.GetName(typeof(TSubscription));

        SubscriptionState state;

        try
        {
            state = await store.Subscriptions
                .GetSubscriptionStateAsync(subscriptionName, token: cancellationToken);
        }
        catch (SubscriptionDoesNotExistException)
        {
            return HealthCheckResult.Unhealthy("Subscription does not exist.");
        }

        if (state.Disabled)
        {
            return HealthCheckResult.Degraded("Subscription is disabled.");
        }

        return HealthCheckResult.Healthy();
    }
}
