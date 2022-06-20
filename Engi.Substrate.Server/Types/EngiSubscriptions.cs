using System.Reactive.Linq;
using System.Text.Json;
using Engi.Substrate.WebSockets;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiSubscriptions : ObjectGraphType
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger logger;

    private Header? lastKnownHeader;

    public EngiSubscriptions(
        IObservable<JsonRpcResponse> chainStream,
        IHttpClientFactory httpClientFactory,
        ILogger<EngiSubscriptions> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;

        AddField(new EventStreamFieldType
        {
            Name = "newHead",
            Type = typeof(HeaderType),
            Resolver = new FuncFieldResolver<Header>(ctx => ctx.Source as Header),
            Subscriber = new EventStreamResolver<Header>(_ =>
            {
                return chainStream
                    .Select(result => Observable.FromAsync(() => ConvertToHeadersAsync(result)))
                    .Concat()
                    .SelectMany(x => x)
                    .AsObservable();
            })
        });
    }

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
            string lastKnownHeaderHash = Hex.GetString0X(lastKnownHeader.ComputeHash());

            while (current.ParentHash != lastKnownHeaderHash)
            {
                current = await client.GetChainHeaderAsync(current.ParentHash);

                // we go backwards

                headers.Insert(0, current);
            }
        }

        lastKnownHeader = header;

        return headers.ToArray();
    }
}