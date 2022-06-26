using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using Engi.Substrate.WebSockets;
using Microsoft.Extensions.Options;

namespace Engi.Substrate.Server;

public class ChainObserverBackgroundService : BackgroundService
{
    private readonly Subject<JsonRpcResponse> updates = new();

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private readonly Uri uri;

    public ChainObserverBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ChainObserverBackgroundService> logger,
        IOptions<SubstrateClientOptions> substrateClientOptions)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;

        uri = new Uri(substrateClientOptions.Value.WsUrl);
    }

    public IObservable<JsonRpcResponse> Updates => updates;

    protected override async Task ExecuteAsync(CancellationToken cancellation)
    {
        var ownRegistrations = new SubscriptionRegistration[]
        {
            new RuntimeSubscriptionRegistration()
        };

        while (!cancellation.IsCancellationRequested)
        {
            // connect and read

            try
            {
                using var connection = await ChainWsConnection.CreateWithRetryAsync(uri, cancellation,
                    (ex, retryInTimeSpan) => logger.LogDebug(ex, "Unable to connect. Retry in: {0}", retryInTimeSpan));

                logger.LogInformation("Connected.");

                // execute registrations

                var registrations = ownRegistrations
                    .Concat(serviceProvider.GetServices<SubscriptionRegistration>())
                    .ToArray();

                var unitializedRegistrations = new Dictionary<long, SubscriptionRegistration>();

                foreach (var registration in registrations)
                {
                    long requestId = await connection.SendJsonAsync(registration.GetPayload(), cancellation);

                    unitializedRegistrations.Add(requestId, registration);
                }

                // start receiving messages

                while (connection.IsOpen)
                {
                    var message = await connection.ReadResponseAsync(cancellation);

                    // initialize any leftover registrations

                    if (unitializedRegistrations.TryGetValue(message.Id, out var registration))
                    {
                        registration.CurrentId = message.Result.GetValue<string>();

                        logger.LogDebug("Registration={type} received subscription id={id}",
                            registration.GetType(), registration.CurrentId);

                        continue;
                    }

                    // look for sub messages

                    if (message.Parameters.SubscriptionId != null)
                    {
                        var match = registrations
                            .FirstOrDefault(x => x.CurrentId == message.Parameters.SubscriptionId);

                        if (match != null)
                        {
                            await match.PublishAsync(message);
                        }

                        continue;
                    }

                    // publish to main stream

                    updates.OnNext(message);
                }
            }
            catch (OperationCanceledException)
                when (cancellation.IsCancellationRequested)
            {
                logger.LogInformation("Cancelled.");

                // only stop if cancelled by us

                updates.OnCompleted();

                return;
            }
            catch (Exception ex)
            {
                bool isNetworkError = ex is IOException or WebSocketException or SocketException;

                logger.Log(isNetworkError ? LogLevel.Debug : LogLevel.Error,
                    ex, "Reconnecting after exception");
            }
        }
    }
}