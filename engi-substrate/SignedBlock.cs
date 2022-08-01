using System.Text.Json.Serialization;

namespace Engi.Substrate;

public class SignedBlock
{
    [JsonPropertyName("block")]
    public Block Block { get; set; } = null!;

    [JsonPropertyName("justifications")]
    public string? Justifications { get; set; }
}