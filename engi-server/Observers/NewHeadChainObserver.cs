using System.Reactive.Subjects;
using System.Text.Json;
using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Server;

public class NewHeadChainObserver : IChainObserver
{
    private readonly Subject<Header> headersSubject = new();
    private readonly Subject<Header> finalizedHeadersSubject = new ();

    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger logger;

    public NewHeadChainObserver(
        IHttpClientFactory httpClientFactory,
        ILogger<NewHeadChainObserver> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public IObservable<Header> Headers => headersSubject;

    public Header? LastHeader { get; private set; }

    public Header? LastFinalizedHeader { get; private set; }

    public JsonRpcRequest[] CreateRequests()
    {
        return new JsonRpcRequest[]
        {
            new()
            {
                Method = ChainKeys.ChainSubscribeNewHead
            },
            new()
            {
                Method = ChainKeys.ChainSubscribeFinalizedHeads
            },
        };
    }

    public async Task ObserveAsync(JsonRpcRequest _, JsonRpcResponse response)
    {
        var header = response.Parameters!.Result.Deserialize<Header>(
            SubstrateJsonSerializerOptions.Default)!;

        if (response.Method == ChainKeys.ChainNewHead)
        {
            LastFinalizedHeader = header;

            finalizedHeadersSubject.OnNext(header);

            return;
        }

        // get header

        var headers = new List<Header>();

        if (LastHeader == null || header.Number != LastHeader.Number)
        {
            headers.Add(header);
        }

        LastHeader ??= header;

        logger.LogDebug($"{DateTime.Now:o} current={header.Number} last={LastHeader.Number} diff={header.Number - LastHeader.Number}");

        if (header.Number - LastHeader.Number > 1)
        {
            // we missed one or more headers

            var client = new SubstrateClient(httpClientFactory);

            var current = header;
            string lastKnownHeaderHash = Hex.GetString0X(LastHeader.ComputeHash());

            while (current.ParentHash != lastKnownHeaderHash)
            {
                current = await client.GetChainHeaderAsync(current.ParentHash);

                // we go backwards

                headers.Insert(0, current);
            }
        }

        LastHeader = header;

        foreach (var item in headers)
        {
            headersSubject.OnNext(item);
        }
    }
}