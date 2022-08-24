using Engi.Substrate.Jobs;
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

        Field<AccountInfoGraphType>("account")
            .Argument<NonNullGraphType<StringGraphType>>("id")
            .ResolveAsync(GetAccountAsync);

        Field<EngiHealthGraphType>("health")
            .ResolveAsync(GetHealthAsync);

        Field<JobGraphType>("job")
            .Argument<NonNullGraphType<ULongGraphType>>("id")
            .ResolveAsync(GetJobAsync);

        Field<JobsPagedResult>("jobs")
            .Argument<JobsPagedQueryArgumentsGraphType>("query")
            .ResolveAsync(GetJobsAsync);

        Field<TransactionsPagedResult>("transactions")
            .Argument<TransactionsPagedQueryArgumentsGraphType>("query")
            .ResolveAsync(GetTransactionsAsync);
    }

    private async Task<object?> GetAccountAsync(IResolveFieldContext context)
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

    private async Task<object?> GetHealthAsync(IResolveFieldContext _)
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

    private async Task<object?> GetJobAsync(IResolveFieldContext context)
    {
        ulong jobId = context.GetArgument<ulong>("id");

        await using var scope = serviceProvider.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var job = await session.LoadAsync<Job>(Job.KeyFrom(jobId));

        return job;
    }

    private async Task<object?> GetJobsAsync(IResolveFieldContext context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var args = context.GetOptionalValidatedArgument<JobsPagedQueryArguments>("query")
            ?? new JobsPagedQueryArguments();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var query = session
            .Query<JobIndex.Result, JobIndex>();

        var results = await query
            .Statistics(out var stats)
            .Skip(args.Skip)
            .Take(args.Limit)
            .As<Job>()
            .ToArrayAsync();

        return new PagedResult<Job>(results, stats.LongTotalResults);
    }

    private async Task<object?> GetTransactionsAsync(IResolveFieldContext context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var args = context.GetValidatedArgument<TransactionsPagedQueryArguments>("query");

        try
        {
            // TODO: make with validation attribute
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
        
        return new PagedResult<TransactionIndex.Result>(results, stats.LongTotalResults);
    }
}