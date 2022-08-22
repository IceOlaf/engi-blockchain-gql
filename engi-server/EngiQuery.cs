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

        Field<EngiHealthGraphType>("health")
            .ResolveAsync(async _ => await GetHealthAsync());

        Field<AccountInfoGraphType>("account")
            .Argument<NonNullGraphType<IdGraphType>>("id")
            .ResolveAsync(async context => await GetAccountAsync(context));

        Field<PagedResultGraphType<TransactionGraphType, TransactionIndex.Result>>("transactions")
            .Argument<TransactionsPagedQueryArgumentsGraphType>("query")
            .ResolveAsync(async context => await GetTransactionsAsync(context));
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

    private async Task<AccountInfo> GetAccountAsync(IResolveFieldContext<object?> context)
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

    private async Task<PagedResult<TransactionIndex.Result>> GetTransactionsAsync(IResolveFieldContext<object?> context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var args = context.GetValidatedArgument<TransactionsPagedQueryArguments>("query");

        try
        {
            Address.From(args.AccountId);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Address is not valid base58.");
        }

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var query = session
            .Query<TransactionIndex.Result, TransactionIndex>()
            .Where(x => x.Executor == args.AccountId || x.OtherParticipants!.Contains(args.AccountId));

        if (args.Type != null)
        {
            query = query.Where(x => x.Type == args.Type);
        }

        var results = await query
            .ProjectInto<TransactionIndex.Result>()
            .Statistics(out var stats)
            .Skip(args.Skip)
            .Take(args.Limit)
            .ToArrayAsync();
        
        return new(results, stats.LongTotalResults);
    }
}