using System.Text;
using System.Text.Json;

namespace System.Net.WebSockets;

public static class ClientWebSocketExtensions
{
    public static Task SendJsonAsync<T>(
        this ClientWebSocket ws,
        T payload,
        CancellationToken cancellation = default,
        JsonSerializerOptions? options = null)
    {
        string json = JsonSerializer.Serialize(payload, options ?? DefaultOptions);

        return ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cancellation);
    }

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };
}