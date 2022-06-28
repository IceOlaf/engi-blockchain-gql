using System.Reactive.Subjects;
using System.Text.Json;
using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Server;

public class NewHeadSubscriptionRegistration : SubscriptionRegistration
{
    private Header? lastKnownHeader;

    private readonly Subject<Header> subject = new();
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger logger;

    public NewHeadSubscriptionRegistration(
        IHttpClientFactory httpClientFactory,
        ILogger<NewHeadSubscriptionRegistration> logger)
        : base("chain_subscribeNewHead")
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public IObservable<Header> Headers => subject;

    public override async Task PublishAsync(JsonRpcResponse result)
    {
        // get header

        var header = result.Parameters.Result.Deserialize<Header>(
            SubstrateJsonSerializerOptions.Default)!;

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
            string lastKnownHeaderHash = Hex.GetString0X(lastKnownHeader.ComputeHash());

            while (current.ParentHash != lastKnownHeaderHash)
            {
                current = await client.GetChainHeaderAsync(current.ParentHash);

                // we go backwards

                headers.Insert(0, current);
            }
        }

        lastKnownHeader = header;

        foreach (var item in headers)
        {
            subject.OnNext(item);
        }
    }
}