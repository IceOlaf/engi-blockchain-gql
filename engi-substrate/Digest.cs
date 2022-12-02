using System.Text.Json.Serialization;

namespace Engi.Substrate;

public class Digest
{
    [JsonPropertyName("logs")]
    public string[] Logs { get; set; } = null!;
}