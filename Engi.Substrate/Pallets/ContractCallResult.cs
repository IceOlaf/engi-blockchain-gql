using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Engi.Substrate.Pallets;

public class ContractCallResult
{
    [JsonPropertyName("Ok")]
    public ContractCallResultOk? Ok { get; set; }

    [JsonPropertyName("Err")]
    public JsonObject? Err { get; set; }
}