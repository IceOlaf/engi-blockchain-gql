using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;

namespace Engi.Substrate.WebSockets;

public class JsonRpcWebSocketObservable
{
    public static IObservable<JsonRpcResponse> Create(
        Uri uri, 
        Func<ClientWebSocket, Task> onReconnect,
        ILogger logger,
        CancellationToken cancellation)
    {
        bool IsCancellationException(Exception ex) =>
            ex is OperationCanceledException && cancellation.IsCancellationRequested;

        var updates = new Subject<JsonRpcResponse>();

        Task.Run(async () =>
        {
            // use a single large buffer

            var buffer = new byte[128 * 1024];

            // continue until cancelled
            
            while (!cancellation.IsCancellationRequested)
            {
                // connect and read

                try
                {
                    var connectPolicy = Policy
                        .Handle<Exception>(ex => !IsCancellationException(ex))
                        .WaitAndRetryForeverAsync(CalculateRetryDelay, 
                            (ex, retryInTimeSpan) => logger.LogDebug(ex, "Unable to connect. Retry in: {0}", retryInTimeSpan));

                    using var ws = await connectPolicy.ExecuteAsync(
                        async () =>
                        {
                            var ws = new ClientWebSocket();

                            await ws.ConnectAsync(uri, cancellation);

                            return ws;
                        });

                    logger.LogInformation("Connected.");

                    await onReconnect(ws);

                    while (ws.State == WebSocketState.Open)
                    {
                        var message = await ReadMessageAsync(ws, buffer, cancellation);

                        var deserialized = JsonSerializer.Deserialize<JsonRpcResponse>(message)!;

                        updates.OnNext(deserialized);
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
        });

        return updates;
    }

    private static async Task<string> ReadMessageAsync(
        ClientWebSocket ws, 
        byte[] buffer, 
        CancellationToken cancellation)
    {
        // clear buffer

        Array.Clear(buffer, 0, buffer.Length);
        var segment = new ArraySegment<byte>(buffer);

        // the cancellation or a network exception will stop the loop

        while (true) 
        {
            var result = await ws.ReceiveAsync(segment, cancellation);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new IOException("Connection closed by WebSocket server.");
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                throw new InvalidOperationException(
                    $"Unexpected message type received: {result.MessageType}.");
            }

            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(buffer, 0, segment.Offset + result.Count);
            }

            // move the segment along

            segment = segment.Slice(result.Count);
        }
    }

    private static TimeSpan CalculateRetryDelay(int @try)
    {
        int delay = @try * 2;

        return TimeSpan.FromSeconds(Math.Min(delay, 30));
    }
}