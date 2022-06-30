using System.Text.Json.Serialization;

namespace Engi.Substrate.WebSockets;

public class JsonRpcRequest
{
    [JsonPropertyName("method")]
    public string Method { get; init; } = null!;

    [JsonPropertyName("params")]
    public string[]? Params { get; init; }

    [JsonIgnore]
    public bool IsSubscription => Method.Contains("_subscribe");
}