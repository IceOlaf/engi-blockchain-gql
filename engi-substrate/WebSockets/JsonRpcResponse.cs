using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Engi.Substrate.WebSockets;

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string Version { get; set; } = null!;

    [JsonPropertyName("method")]
    public string Method { get; set; } = null!;

    [JsonPropertyName("result")]
    public JsonNode Result { get; set; } = null!;

    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("params")]
    public JsonRpcResponseParameters? Parameters { get; set; } = null!;

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

    public override string ToString()
    {
        return $"{Id}: {Result}";
    }
}