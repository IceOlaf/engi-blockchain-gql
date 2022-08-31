using Engi.Substrate.Indexing;
using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Indexing;
using Engi.Substrate.Server.Types;
using Engi.Substrate.Server.Types.Validation;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries;
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
            .Argument<JobsQueryArgumentsGraphType>("query")
            .ResolveAsync(GetJobsAsync);

        Field<TransactionsPagedResult>("transactions")
            .Argument<TransactionsPagedQueryArgumentsGraphType>("query")
            .ResolveAsync(GetTransactionsAsync);
    }

    private async Task<object?> GetAccountAsync(IResolveFieldContext context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        string id = context.GetValidatedArgument<string>("id", new AccountIdAttribute());

        var address = Address.Parse(id);

        try
        {
            return await substrate.GetSystemAccountAsync(address);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
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

        var reference = await session
            .LoadAsync<ReduceOutputReference>(JobIndex.ReferenceKeyFrom(jobId),
                include => include.IncludeDocuments<ReduceOutputReference>(x => x.ReduceOutputs));

        if (reference == null)
        {
            return null;
        }

        return session.LoadAsync<Job>(reference.ReduceOutputs.First()).Result;
    }

    private async Task<object?> GetJobsAsync(IResolveFieldContext context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var args = context.GetOptionalValidatedArgument<JobsQueryArguments>("query")
            ?? new JobsQueryArguments();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var query = session
            .Advanced.AsyncDocumentQuery<JobIndex.Result, JobIndex>();

        if (args.Creator != null)
        {
            query = query
                .WhereEquals(x => x.Creator, args.Creator);
        }

        if (args.Status.HasValue)
        {
            query = query
                .WhereEquals(x => x.Status, args.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(args.Search))
        {
            query = query
                .Search(x => x.Query, $"{args.Search}*", @operator: SearchOperator.And);
        }

        if (args.Language.HasValue)
        {
            query = query
                .WhereIn(x => x.Language, new [] { args.Language.Value });
        }

        if (args.MinFunding != null && args.MaxFunding != null)
        {
            query = query
                .WhereBetween(x => x.Funding, 
                    args.MinFunding.Value.ToString("D40"), args.MaxFunding.Value.ToString("D40"));
        }
        else if (args.MinFunding != null)
        {
            query = query
                .WhereGreaterThanOrEqual(x => x.Funding, args.MinFunding.Value.ToString("D40"));
        }
        else if (args.MaxFunding != null)
        {
            query = query
                .WhereLessThanOrEqual(x => x.Funding, args.MaxFunding.Value.ToString("D40"));
        }

        switch (args.OrderByProperty)
        {
            case JobsOrderByProperty.CreatedOn:
                query = args.OrderByDirection == OrderByDirection.Asc
                    ? query.OrderBy(x => x.CreatedOn.DateTime)
                    : query.OrderByDescending(x => x.CreatedOn.DateTime);
                break;

            case JobsOrderByProperty.Funding:
                query = args.OrderByDirection == OrderByDirection.Asc
                    ? query.OrderBy(x => x.Funding)
                    : query.OrderByDescending(x => x.Funding);
                break;
        }

        var results = await query
            .Statistics(out var stats)
            .Skip(args.Skip)
            .Take(args.Limit)
            .OfType<Job>()
            .ToArrayAsync();

        return new PagedResult<Job>(results, stats.LongTotalResults);
    }

    private async Task<object?> GetTransactionsAsync(IResolveFieldContext context)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var args = context.GetValidatedArgument<TransactionsPagedQueryArguments>("query");

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