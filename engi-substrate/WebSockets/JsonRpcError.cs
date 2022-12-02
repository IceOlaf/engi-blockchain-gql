using System.Text.Json.Serialization;

namespace Engi.Substrate.WebSockets;

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public long Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
}