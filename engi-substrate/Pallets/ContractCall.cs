using System.Text.Json.Serialization;

namespace Engi.Substrate.Pallets;

public class ContractCall
{
    [JsonPropertyName("dest")]
    public string ContractAddress { get; set; } = null!;

    [JsonPropertyName("origin")]
    public string Origin { get; set; } = null!;

    [JsonPropertyName("gasLimit")]
    public ulong GasLimit { get; set; }

    [JsonPropertyName("storageDepositLimit")]
    public ulong? StorageDepositLimit { get; set; }

    [JsonPropertyName("value")]
    public ulong Value { get; set; }

    [JsonPropertyName("inputData")]
    public string InputData0X { get; set; } = null!;
}