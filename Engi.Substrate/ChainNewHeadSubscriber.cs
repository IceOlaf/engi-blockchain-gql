using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text.Json;
using Engi.Substrate.WebSockets;
using Microsoft.Extensions.Logging;

namespace Engi.Substrate;

public class ChainNewHeadSubscriber : IObservable<Header>
{
    private readonly Uri wssUri;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<ChainNewHeadSubscriber> logger;
    private readonly IObservable<Header> stream;

    private Header? lastKnownHeader;

    public ChainNewHeadSubscriber(
        Uri wssUri,
        IHttpClientFactory httpClientFactory,
        ILogger<ChainNewHeadSubscriber> logger)
    {
        this.wssUri = wssUri;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;

        stream = JsonRpcWebSocketObservable.Create(wssUri, SubscribeToNewHeadsAsync, logger, default)
            .Select(result => Observable.FromAsync(() => ConvertToHeadersAsync(result)))
            .Concat()
            .SelectMany(x => x)
            .AsObservable();
    }

    public IDisposable Subscribe(IObserver<Header> observer) => stream.Subscribe(observer);

    private Task SubscribeToNewHeadsAsync(ClientWebSocket ws) => ws.SendJsonAsync(new
    {
        id = 1,
        jsonrpc = "2.0",
        method = "chain_subscribeNewHead"
    });

    private async Task<Header[]> ConvertToHeadersAsync(JsonRpcResponse result)
    {
        if (result.Method != "chain_newHead")
        {
            return Array.Empty<Header>();
        }

        // get header

        var header = JsonSerializer.Deserialize<Header>(result.Parameters.Result)!;

        var headers = new List<Header>();

        if (lastKnownHeader == null || header.Number != lastKnownHeader.Number)
        {
            headers.Add(header);
        }

        lastKnownHeader ??= header;

        logger.LogTrace($"{DateTime.Now:o} current={header.Number} last={lastKnownHeader.Number} diff={header.Number - lastKnownHeader.Number}");

        if (header.Number - lastKnownHeader.Number > 1)
        {
            // we missed one or more headers

            var client = new SubstrateClient(httpClientFactory);

            var current = header;
            string lastKnownHeaderHash = Hex.GetString0x(lastKnownHeader.ComputeHash());

            while (current.ParentHash != lastKnownHeaderHash)
            {
                current = await client.GetHeaderAsync(current.ParentHash);

                // we go backwards

                headers.Insert(0, current);
            }
        }

        lastKnownHeader = header;

        return headers.ToArray();
    }
}