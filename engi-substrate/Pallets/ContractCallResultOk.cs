using System.Text.Json.Serialization;

namespace Engi.Substrate.Pallets;

public class ContractCallResultOk
{
    [JsonPropertyName("data")]
    public string Data0X { get; set; } = null!;

    [JsonPropertyName("flags")]
    public ulong Flags { get; set; }
}