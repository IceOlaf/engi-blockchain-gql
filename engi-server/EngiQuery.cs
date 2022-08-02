using Engi.Substrate.Pallets;
using Engi.Substrate.Server.Indexing;
using Engi.Substrate.Server.Types;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Sentry;

namespace Engi.Substrate.Server;

public class EngiQuery : ObjectGraphType
{
    private readonly IServiceProvider serviceProvider;

    public EngiQuery(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        FieldAsync<EngiHealthGraphType>("health", resolve: async _ => await GetHealthAsync());

        FieldAsync<AccountInfoGraphType>("account",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id" }
            ),
            resolve: async context => await GetAccountAsync(context));

        FieldAsync<PagedResultGraphType<TransactionGraphType, TransactionIndex.Result>>("transactions",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id" },
                new QueryArgument<EnumerationGraphType<TransactionType>> { Name = "type" },
                new QueryArgument<NonNullGraphType<UIntGraphType>> { Name = "skip", DefaultValue = 0 },
                new QueryArgument<NonNullGraphType<UIntGraphType>> { Name = "limit", DefaultValue = 25 }
            ),
            resolve: async context => await GetTransactionsAsync(context));
    }

    private async Task<EngiHealth> GetHealthAsync()
    {
        using var scope = serviceProvider.CreateScope();

        var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();
        var sentry = scope.ServiceProvider.GetRequiredService<IHub>();

        try
        {
            var chainTask = substrate.GetSystemChainAsync();
            var nameTask = substrate.GetSystemNameAsync();
            var versionTask = substrate.GetSystemVersionAsync();
            var healthTask = substrate.GetSystemHealthAsync();

            await Task.WhenAll(
                chainTask,
                nameTask,
                versionTask,
                healthTask
            );

            return new EngiHealth
            {
                Chain = chainTask.Result,
                NodeName = nameTask.Result,
                Version = versionTask.Result,
                Status = healthTask.Result.Peers > 0 ? EngiHealthStatus.Online : EngiHealthStatus.Offline,
                PeerCount = healthTask.Result.Peers
            };
        }
        catch (Exception ex)
        {
            sentry.CaptureException(ex);

            return new EngiHealth
            {
                Status = EngiHealthStatus.Offline
            };
        }
    }

    private async Task<AccountInfo> GetAccountAsync(IResolveFieldContext<object> context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        string accountId = context.GetArgument<string>("id")!;

        Address address;

        try
        {
            address = Address.From(accountId);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Address is not valid base58.");
        }

        return await substrate.GetSystemAccountAsync(address);
    }

    private async Task<PagedResult<TransactionIndex.Result>> GetTransactionsAsync(IResolveFieldContext<object> context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        string accountId = context.GetArgument<string>("id")!;
        var type = context.GetArgument<TransactionType?>("type");
        int skip = checked((int)context.GetArgument<uint>("skip"));
        int limit = checked((int)context.GetArgument<uint>("limit"));

        try
        {
            Address.From(accountId);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Address is not valid base58.");
        }

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var query = session
            .Query<TransactionIndex.Result, TransactionIndex>()
            .Where(x => x.Executor == accountId || x.OtherParticipants!.Contains(accountId));

        if (type != null)
        {
            query = query.Where(x => x.Type == type);
        }

        var results = await query
            .ProjectInto<TransactionIndex.Result>()
            .Statistics(out var stats)
            .Skip(skip)
            .Take(limit)
            .ToArrayAsync();
        
        return new(results, stats.LongTotalResults);
    }
}