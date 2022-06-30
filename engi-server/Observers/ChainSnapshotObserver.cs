using System.Text.Json;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Server;

public class ChainSnapshotObserver : IChainObserver
{
    public RuntimeMetadata Metadata { get; private set; } = null!;

    public RuntimeVersion Version { get; private set; } = null!;

    public string GenesisHash { get; private set; } = null!;

    public JsonRpcRequest[] CreateRequests()
    {
        return new JsonRpcRequest[]
        {
            new()
            {
                Method = ChainKeys.StateGetMetadata
            },
            new()
            {
                Method = ChainKeys.ChainGetBlockHash,
                Params = new[] { "0" }
            },
            new()
            {
                Method = ChainKeys.StateSubscribeRuntimeVersion
            }
        };
    }

    public Task ObserveAsync(JsonRpcRequest request, JsonRpcResponse response)
    {
        if (request.Method == ChainKeys.StateGetMetadata)
        {
            using var stream = new ScaleStreamReader(response.Result.GetValue<string>());

            Metadata = RuntimeMetadata.Parse(stream);
        }
        else if (response.Method == ChainKeys.StateRuntimeVersion)
        {
            Version = response.Parameters!.Result.Deserialize<RuntimeVersion>(
                SubstrateJsonSerializerOptions.Default)!;
        }
        else if (request.Method == ChainKeys.ChainGetBlockHash && request.Params![0] == "0")
        {
            GenesisHash = response.Result.GetValue<string>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Unrecognized response; json={JsonSerializer.SerializeToNode(response)}");
        }

        return Task.CompletedTask;
    }
}