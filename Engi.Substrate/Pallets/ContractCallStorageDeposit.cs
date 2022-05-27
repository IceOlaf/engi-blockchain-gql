using System.Text.Json.Serialization;

namespace Engi.Substrate.Pallets;

public class ContractCallStorageDeposit
{
    [JsonPropertyName("charge")]
    public string Charge0X { get; set; } = null!;
}