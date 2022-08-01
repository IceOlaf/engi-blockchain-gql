using System.Text.Json.Serialization;

namespace Engi.Substrate;

public class Block
{
    [JsonPropertyName("header")]
    public Header Header { get; set; } = null!;

    [JsonPropertyName("extrinsics")]
    public string[] Extrinsics { get; set; } = null!;
}