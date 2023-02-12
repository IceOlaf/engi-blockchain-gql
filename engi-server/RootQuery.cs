using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Indexing;
using Engi.Substrate.Server.Types;
using Engi.Substrate.Server.Types.Github;
using Engi.Substrate.Server.Types.Validation;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries.Suggestions;
using Raven.Client.Documents.Session;
using Sentry;

namespace Engi.Substrate.Server;

public class RootQuery : ObjectGraphType
{
    public RootQuery()
    {
        this.AllowAnonymous();

        Field<AccountInfoGraphType>("account")
            .Argument<NonNullGraphType<StringGraphType>>("id")
            .ResolveAsync(GetAccountAsync);

        Field<AccountsQuery>("accounts")
            .Resolve(_ => new { });

        Field<AuthQuery>("auth")
            .Resolve(_ => new { });

        Field<GithubQuery>("github")
            .Resolve(_ => new { });

        Field<EngiHealthGraphType>("health")
            .ResolveAsync(GetHealthAsync);

        Field<JobDetailsGraphType>("job")
            .Argument<NonNullGraphType<UInt64GraphType>>("id")
            .ResolveAsync(GetJobAsync);

        Field<JobsQueryResultGraphType>("jobs")
            .Argument<JobsQueryArgumentsGraphType>("query")
            .ResolveAsync(GetJobsAsync);

        Field<JobAggregatesGraphType>("jobAggregates")
            .ResolveAsync(GetJobAggregatesAsync);

        Field<TransactionsPagedResult>("transactions")
            .Argument<TransactionsPagedQueryArgumentsGraphType>("query")
            .ResolveAsync(GetTransactionsAsync);
    }

    private async Task<object?> GetAccountAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

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

    private async Task<object?> GetHealthAsync(IResolveFieldContext context)
    {
        using var scope = context.RequestServices!.CreateScope();

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
            if (!ExceptionUtils.IsTransient(ex))
            {
                sentry.CaptureException(ex);
            }

            return new EngiHealth
            {
                Status = EngiHealthStatus.Offline
            };
        }
    }

    private async Task<object?> GetJobAsync(IResolveFieldContext context)
    {
        ulong jobId = context.GetArgument<ulong>("id");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var reference = await session
            .LoadAsync<ReduceOutputReference>(JobIndex.ReferenceKeyFrom(jobId),
                include => include.IncludeDocuments(x => x.ReduceOutputs));

        if (reference == null)
        {
            return null;
        }

        var job = await session
            .LoadAsync<Job>(reference.ReduceOutputs.First(),
                include => include.IncludeDocuments<JobUserAggregatesIndex.Result>(x => $"JobUserAggregates/{x.Creator}"));

        var creatorAggregatesReference = await session
            .LoadAsync<ReduceOutputReference>(
                JobUserAggregatesIndex.Result.ReferenceKeyFrom(job.Creator),
                include => include.IncludeDocuments(x => x.ReduceOutputs));

        JobUserAggregatesIndex.Result? creatorAggregates = null;

        if (creatorAggregatesReference?.ReduceOutputs.Any() == true)
        {
            creatorAggregates = await session
                .LoadAsync<JobUserAggregatesIndex.Result>(
                    creatorAggregatesReference.ReduceOutputs.FirstOrDefault());
        }

        return new JobDetails
        {
            Job = job,
            CreatorUserInfo = new()
            {
                Address = job.Creator,
                CreatedJobsCount = creatorAggregates?.CreatedCount ?? 0,
                SolvedJobsCount = creatorAggregates?.SolvedCount ?? 0
            }
        };
    }

    private async Task<object?> GetJobsAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        var args = context.GetOptionalValidatedArgument<JobsQueryArguments>("query");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var query = session
            .Advanced.AsyncDocumentQuery<JobIndex.Result, JobIndex>()
            .FilterBy(args, out var stats);

        Lazy<Task<Dictionary<string, SuggestionResult>>>? suggestionsLazy = null;
        
        if (!string.IsNullOrEmpty(args?.Search))
        {
            suggestionsLazy = session
                .Advanced.AsyncDocumentQuery<JobIndex.Result, JobIndex>()
                .SuggestUsing(x => x
                    .ByField(r => r.Query, args.Search)
                    .WithOptions(new()
                    {
                        SortMode = SuggestionSortMode.Popularity
                    }))
                .ExecuteLazyAsync();
        }

        var resultsLazy = query
            .Include(x => x.SolutionIds)
            .LazilyAsync();

        await session.Advanced.Eagerly
            .ExecuteAllPendingLazyOperationsAsync();

        var solutionsByJobId = resultsLazy.Value.Result
            .ToDictionary(x => x.JobId, x => session.LoadAsync<SolutionSnapshot>(x.SolutionIds).Result.Values);

        foreach (var job in resultsLazy.Value.Result)
        {
            var solutions = solutionsByJobId[job.JobId];

            job.PopulateSolutions(null, solutions);
        }

        return new JobsQueryResult
        {
            Result = new PagedResult<Job>(resultsLazy.Value.Result, stats.LongTotalResults),
            Suggestions = suggestionsLazy?.Value.Result.Values.First().Suggestions.ToArray()
        };
    }

    private async Task<object?> GetJobAggregatesAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var aggregates = await session
            .Query<JobAggregateIndex.Result, JobAggregateIndex>()
            .ProjectInto<JobAggregateIndex.Result>()
            .FirstOrDefaultAsync();

        return aggregates ?? new JobAggregateIndex.Result();
    }

    private async Task<object?> GetTransactionsAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

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
