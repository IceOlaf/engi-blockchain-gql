using System.Text.Json.Serialization;

namespace Engi.Substrate.Pallets;

public class ContractCallResponse
{
    [JsonPropertyName("debugMessage")]
    public string DebugMessage { get; set; } = null!;

    [JsonPropertyName("gasConsumed")]
    public decimal GasConsumed { get; set; }

    [JsonPropertyName("gasRequired")]
    public decimal GasRequired { get; set; }

    [JsonPropertyName("storageDeposit")]
    public ContractCallStorageDeposit StorageDeposit { get; set; } = null!;

    [JsonPropertyName("result")]
    public ContractCallResult Result { get; set; } = null!;
}
