using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Engi.Substrate.WebSockets;

public class JsonRpcResponseParameters
{
    [JsonPropertyName("result")]
    public JsonNode Result { get; set; } = null!;

    [JsonPropertyName("subscription")]
    public string? SubscriptionId { get; set; } = null!;
}