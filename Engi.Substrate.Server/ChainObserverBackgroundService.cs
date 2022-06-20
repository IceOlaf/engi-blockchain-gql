using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using Engi.Substrate.WebSockets;
using Microsoft.Extensions.Options;

namespace Engi.Substrate.Server;

public class ChainObserverBackgroundService : BackgroundService
{
    private readonly Subject<JsonRpcResponse> updates = new();

    private readonly ILogger logger;
    private readonly Uri uri;

    public ChainObserverBackgroundService(
        ILogger<ChainObserverBackgroundService> logger,
        IOptions<SubstrateClientOptions> substrateClientOptions)
    {
        this.logger = logger;

        uri = substrateClientOptions.Value.WssUri;
    }

    public IObservable<JsonRpcResponse> Updates => updates;

    private async Task OnReconnect(ChainWsConnection connection)
    {
        await connection.SendJsonAsync(new
        {
            method = "chain_subscribeNewHead"
        }, default);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            // connect and read

            try
            {
                using var connection = await ChainWsConnection.CreateWithRetryAsync(uri, cancellation,
                    (ex, retryInTimeSpan) => logger.LogDebug(ex, "Unable to connect. Retry in: {0}", retryInTimeSpan));

                logger.LogInformation("Connected.");

                await OnReconnect(connection);

                while (connection.IsOpen)
                {
                    var message = await connection.ReadResponseAsync(cancellation);

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