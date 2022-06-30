using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text.Json;
using Engi.Substrate.WebSockets;
using Microsoft.Extensions.Options;

namespace Engi.Substrate.Server;

public class ChainObserverBackgroundService : BackgroundService
{
    private readonly Subject<JsonRpcResponse> updates = new();
    private readonly Dictionary<long, RoutingEntry> requestRoutes = new();
    private readonly Dictionary<string, RoutingEntry> subscriptionRoutes = new();

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly Uri uri;

    public ChainObserverBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ChainObserverBackgroundService> logger,
        ILoggerFactory loggerFactory,
        IOptions<SubstrateClientOptions> substrateClientOptions)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.loggerFactory = loggerFactory;

        uri = new Uri(substrateClientOptions.Value.WsUrl);
    }

    public IObservable<JsonRpcResponse> Updates => updates;

    class RoutingEntry
    {
        public JsonRpcRequest Request { get; init; } = null!;
        public IChainObserver Observer { get; init; } = null!;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            // connect and read

            try
            {
                using var connection = await ChainWsConnection.CreateWithRetryAsync(uri, loggerFactory, cancellation,
                    (ex, retryInTimeSpan) => logger.LogDebug(ex, "Unable to connect. Retry in: {0}", retryInTimeSpan));

                logger.LogInformation("Connected to {url}.", uri);

                // initialize observers

                var observers = serviceProvider.GetServices<IChainObserver>().ToArray();

                requestRoutes.Clear();
                subscriptionRoutes.Clear();
                
                foreach (var observer in observers)
                {
                    foreach (var request in observer.CreateRequests())
                    {
                        long requestId = await connection.SendJsonAsync(request, cancellation);

                        // if the request is a subscription, wait for the response now
                        // otherwise subscription messages can come before the actual
                        // subscribe response and the system cant find the relevant observer

                        if (request.IsSubscription)
                        {
                            var unprocessedQueue = new List<JsonRpcResponse>();

                            while (true)
                            {
                                var response = await connection.ReadResponseAsync(cancellation);

                                if (requestId == response.Id)
                                {
                                    string subscriptionKey = response.Result.GetValue<string>();

                                    subscriptionRoutes!.Add(subscriptionKey, new()
                                    {
                                        Request = request,
                                        Observer = observer
                                    });

                                    logger.LogDebug("Registration={type} received subscription key={key}",
                                        observer.GetType(), subscriptionKey);

                                    break;
                                }

                                unprocessedQueue.Add(response);
                            }

                            foreach (var response in unprocessedQueue)
                            {
                                await ProcessAsync(response);
                            }
                        }
                        else
                        {
                            requestRoutes.Add(requestId, new()
                            {
                                Observer = observer,
                                Request = request
                            });
                        }
                    }
                }
                
                // start receiving messages

                while (connection.IsOpen)
                {
                    var response = await connection.ReadResponseAsync(cancellation);

                    await ProcessAsync(response);
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

    private async Task ProcessAsync(JsonRpcResponse response)
    {
        // if the message has an id, it needs to be routed to the request table

        if (response.Id != null)
        {
            bool found = requestRoutes!.Remove(response.Id.Value, out var state);

            if (!found)
            {
                logger.LogWarning("No match for request id={id}", response.Id);

                return;
            }

            // if the request was to subscribe, must add to sub routing table

            if (state!.Request.IsSubscription)
            {

            }
            else
            {
                // pass to observer

                await state.Observer.ObserveAsync(state.Request, response);
            }
        }
        else
        {
            // if not id, it must be a response to a sub

            string? subscriptionId = response.Parameters!.SubscriptionId;

            if (string.IsNullOrEmpty(subscriptionId))
            {
                logger.LogError("Expected a subscription but subscription id is null; json={json}",
                    JsonSerializer.Serialize(response));

                return;
            }

            bool found = subscriptionRoutes!.TryGetValue(subscriptionId, out var state);

            if (!found)
            {
                logger.LogWarning("Couldn't match observer for subscription id={id}",
                    response.Parameters.SubscriptionId);

                return;
            }

            await state!.Observer.ObserveAsync(state.Request, response);
        }
    }
}