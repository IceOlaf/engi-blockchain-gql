using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Polly;

namespace Engi.Substrate.WebSockets;

public class ChainWsConnection : IDisposable
{
    private static long IdCounter = 0;

    private readonly Uri uri;
    private readonly ClientWebSocket ws = new();
    private readonly byte[] buffer = new byte[128 * 1024];

    public ChainWsConnection(Uri uri)
    {
        this.uri = uri;
    }

    public bool IsOpen => ws.State == WebSocketState.Open;

    public Task ConnectAsync(CancellationToken cancellation) => ws.ConnectAsync(uri, cancellation);

    public async Task<string> ReadMessageAsync(CancellationToken cancellation)
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

    public async Task<JsonRpcResponse> ReadResponseAsync(CancellationToken cancellation)
    {
        string json = await ReadMessageAsync(cancellation);

        return JsonSerializer.Deserialize<JsonRpcResponse>(json)!;
    }

    public Task SendJsonAsync<T>(T payload, CancellationToken cancellation)
    {
        long id = Interlocked.Increment(ref IdCounter);

        var json = JsonSerializer.SerializeToNode(payload, DefaultOptions)!;

        json["id"] = id;
        json["jsonrpc"] = "2.0";

        string jsonString = json.ToJsonString();

        byte[] bytes = Encoding.UTF8.GetBytes(jsonString);

        return ws.SendAsync(bytes, WebSocketMessageType.Text, true, cancellation);
    }

    public void Dispose()
    {
        ws.Dispose();
    }

    public static async Task<ChainWsConnection> CreateAsync(Uri uri, CancellationToken cancellation)
    {
        var connection = new ChainWsConnection(uri);

        await connection.ConnectAsync(cancellation);

        return connection;
    }

    public static Task<ChainWsConnection> CreateWithRetryAsync(
        Uri uri, 
        CancellationToken cancellation,
        Action<Exception, TimeSpan>? onRetry = null)
    {
        bool IsCancellationException(Exception ex) =>
            ex is OperationCanceledException && cancellation.IsCancellationRequested;

        var connectPolicy = Policy
            .Handle<Exception>(ex => !IsCancellationException(ex))
            .WaitAndRetryForeverAsync(CalculateRetryDelay, (ex, retryTimeSpan) => onRetry?.Invoke(ex, retryTimeSpan));

        return connectPolicy.ExecuteAsync(
            () => CreateAsync(uri, cancellation));
    }

    private static TimeSpan CalculateRetryDelay(int @try)
    {
        int delay = @try * 2;

        return TimeSpan.FromSeconds(Math.Min(delay, 30));
    }

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };
}