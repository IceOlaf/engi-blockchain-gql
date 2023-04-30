using System.Text.Json;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Observers;

public class ChainSnapshotObserver : IChainObserver
{
    private TaskCompletionSource<RuntimeMetadata> metadataCompletion = new();
    private TaskCompletionSource<RuntimeVersion> versionCompletion = new();
    private TaskCompletionSource<string> genesisHashCompletion = new();
    
    public Task<RuntimeMetadata> Metadata => metadataCompletion.Task;

    public Task<RuntimeVersion> Version => versionCompletion.Task;

    public Task<string> GenesisHash => genesisHashCompletion.Task;

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

            SetResultOrRecreate(ref metadataCompletion, RuntimeMetadata.Parse(stream));
        }
        else if (response.Method == ChainKeys.StateRuntimeVersion)
        {
            var version = response.Parameters!.Result.Deserialize<RuntimeVersion>(
                SubstrateJsonSerializerOptions.Default)!;

            SetResultOrRecreate(ref versionCompletion, version);
        }
        else if (request.Method == ChainKeys.ChainGetBlockHash && request.Params![0] == "0")
        {
            SetResultOrRecreate(ref genesisHashCompletion, response.Result.GetValue<string>());
        }
        else
        {
            throw new InvalidOperationException(
                $"Unrecognized response; json={JsonSerializer.SerializeToNode(response)}");
        }

        return Task.CompletedTask;
    }

    private static void SetResultOrRecreate<T>(ref TaskCompletionSource<T> completion, T value)
    {
        if (completion.Task.IsCompleted)
        {
            completion = new TaskCompletionSource<T>();
        }

        completion.SetResult(value);
    }
}
