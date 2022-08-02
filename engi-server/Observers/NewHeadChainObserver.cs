using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text.Json;
using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Server;

public class NewHeadChainObserver : IChainObserver
{
    private readonly Subject<Header> finalizedHeadersSubject = new ();

    public IObservable<Header> FinalizedHeaders => finalizedHeadersSubject;

    public Header? LastFinalizedHeader { get; private set; }

    public JsonRpcRequest[] CreateRequests()
    {
        return new JsonRpcRequest[]
        {
            new()
            {
                Method = ChainKeys.ChainSubscribeFinalizedHeads
            }
        };
    }

    public Task ObserveAsync(JsonRpcRequest _, JsonRpcResponse response)
    {
        var header = response.Parameters!.Result.Deserialize<Header>(
            SubstrateJsonSerializerOptions.Default)!;

        Debug.Assert(response.Method == ChainKeys.ChainFinalizedHead);

        LastFinalizedHeader = header;

        finalizedHeadersSubject.OnNext(header);

        return Task.CompletedTask;
    }
}