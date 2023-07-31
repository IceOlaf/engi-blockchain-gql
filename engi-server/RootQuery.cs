using System.Linq.Expressions;
using Engi.Substrate.Indexing;
using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Async;
using Engi.Substrate.Server.Types;
using Engi.Substrate.Server.Types.Github;
using Engi.Substrate.Server.Types.Validation;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Queries.Suggestions;
using Raven.Client.Documents.Session;
using Sentry;

using User = Engi.Substrate.Identity.User;

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

        Field<ActivityGraphType>("activity")
            .Argument<ActivityArgumentsGraphType>("args")
            .ResolveAsync(GetActivityAsync)
            .Description("Get the job activity for the last N days.");

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

        Field<JobSubmissionsGraphType>("submissions")
            .Argument<NonNullGraphType<UInt64GraphType>>("id")
            .ResolveAsync(GetJobSubmissionsAsync);
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

    private async Task<object?> GetActivityAsync(IResolveFieldContext context)
    {
        var args = context.GetOptionalValidatedArgument<ActivityArguments>("args") ?? new();

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var queries = new List<(string Day, Lazy<Task<IEnumerable<Job>>> Completed, Lazy<Task<IEnumerable<Job>>> NotCompleted)>();

        foreach(var day in Enumerable.Range(0, args.DayCount)
            .Select(offset => DateTime.UtcNow.AddDays(-offset).ToString("yyyy-MM-dd")))
        {
            var @base = session
                .Query<JobIndex.Result, JobIndex>()
                .Where(x => x.UpdatedOn_Date == day)
                .OrderByDescending(x => x.UpdatedOn_DateTime);

            var completed = @base
                .Where(x => x.Status == JobStatus.Complete)
                .Take(args.MaxCompletedCount)
                .ProjectInto<Job>()
                .LazilyAsync();

            var notCompleted = @base
                .Where(x => x.Status.In(JobStatus.Open, JobStatus.Active))
                .Take(args.MaxNotCompletedCount)
                .ProjectInto<Job>()
                .LazilyAsync();

            queries.Add((day, completed, notCompleted));
        }

        await session.Advanced.Eagerly
            .ExecuteAllPendingLazyOperationsAsync();

        var items = queries.Select(x => new ActivityDaily
        {
            Date = DateTime.ParseExact(x.Day, "yyyy-MM-dd", null),
            Completed = x.Completed.Value.Result,
            NotCompleted = x.NotCompleted.Value.Result
        });

        return new Activity
        {
            Items = items
                .OrderBy(x => x.Date)
        };
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

        var index = await session
            .Query<JobIndex.Result, JobIndex>()
            .Where(x => x.JobId == jobId)
            .ProjectInto<JobIndex.Result>()
            .FirstOrDefaultAsync();

        var job = await session
            .LoadAsync<Job>(reference.ReduceOutputs.First(),
                include => include.IncludeDocuments<JobUserAggregatesIndex.Result>(x => $"JobUserAggregates/{x.Creator}"));

        if (index != null && index.AttemptIds.Length > 0)
        {
            var tasks = index.AttemptIds.Select( async id => await GetJobSubmissionsDetailsAsync(Convert.ToUInt64(id, 10), session) ).ToList();
            var completed = await Task.WhenAll(tasks);
            List<JobSubmissionsDetails> submissions = completed.OfType<JobSubmissionsDetails>().ToList();
            Console.WriteLine("Lisst of Job Singgs ");
            Console.WriteLine(submissions.Count);
            Console.WriteLine("Lisst of Job Singgs ");

            job.PopulateSubmissions(submissions);
        }

        var creatorAggregatesReference = await session
            .LoadAsync<ReduceOutputReference>(
                JobUserAggregatesIndex.Result.ReferenceKeyFrom(job.Creator),
                include => include.IncludeDocuments(x => x.ReduceOutputs));

        JobUserAggregatesIndex.Result? creatorAggregates = null;
        User? creator = null;

        if (creatorAggregatesReference?.ReduceOutputs.Any() == true)
        {
            creatorAggregates = await session
                .LoadAsync<JobUserAggregatesIndex.Result>(
                    creatorAggregatesReference.ReduceOutputs.FirstOrDefault(),
                    include => include.IncludeDocuments(x => x.UserId));

            if (creatorAggregates?.UserId != null)
            {
                creator = await session.LoadAsync<User>(creatorAggregates.UserId);
            }
        }

        return new JobDetails
        {
            Job = job,
            CreatorUserInfo = new()
            {
                Address = job.Creator,
                Display = creator?.Display,
                ProfileImageUrl = creator?.ProfileImageUrl,
                CreatedOn = creator?.CreatedOn,
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
            .Search(args, out var stats);

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
            .Include(x => x.AttemptIds)
            .LazilyAsync();

        var now = DateTime.UtcNow;

        var createdOnRanges = new[]
        {
            new { Period = "LastDay", DateTime = now.AddDays(-1).Date },
            new { Period = "Last15", DateTime = now.AddDays(-15).Date },
            new { Period = "Last30", DateTime = now.AddDays(-30).Date },
            new { Period = "LastQuarter", DateTime = now.AddDays(-120).Date },
            new { Period = "LastYear", DateTime = now.AddYears(-1).Date }
        };

        var facetsLazy = query
            .AggregateBy(builder => builder.ByField(x => x.Technologies))
            .AndAggregateBy(builder => builder.ByField(x => x.Repository_FullName))
            .AndAggregateBy(builder => builder.ByField(x => x.Repository_Organization))
            .AndAggregateBy(new RangeFacet<JobIndex.Result>
            {
                Ranges = createdOnRanges
                    .Select(range =>
                    {
                        var dt = range.DateTime;
                        Expression<Func<JobIndex.Result, bool>> exp = x => x.CreatedOn_DateTime >= dt;
                        return exp;
                    })
                    .ToList()
            })
            .ExecuteLazyAsync();
        Console.WriteLine("DuStoopheaux");

        await session.Advanced.Eagerly
            .ExecuteAllPendingLazyOperationsAsync();

        Console.WriteLine("DurrpStoopheaux");
        var solutionsByJobId = resultsLazy.Value.Result
            .ToDictionary(x => x.JobId, x => session.LoadAsync<SolutionSnapshot>(x.SolutionIds).Result.Values);

        Console.WriteLine("SlurrpDurrpStoopheaux");
        var submissionsByJobId = resultsLazy.Value.Result
            .ToDictionary(
                x => x.JobId,
                x => x.AttemptIds.Select( async id => await GetJobSubmissionsDetailsAsync(Convert.ToUInt64(id, 10), session) ).ToList()
            );
        Console.WriteLine("DehDuStoopheaux");

        foreach (var job in resultsLazy.Value.Result)
        {
            var solutions = solutionsByJobId[job.JobId];
            var completed = await Task.WhenAll(submissionsByJobId[job.JobId]);
            List<JobSubmissionsDetails> submissions = completed.OfType<JobSubmissionsDetails>().ToList();

            job.PopulateSolutions(null, solutions);
            job.PopulateSubmissions(submissions);
        }

        Console.WriteLine("DehDuStoopheaux");
        return new JobsQueryResult
        {
            Result = new PagedResult<Job>(resultsLazy.Value.Result, stats.LongTotalResults),
            Suggestions = suggestionsLazy?.Value.Result.Values.First().Suggestions.ToArray(),
            Facets = new()
            {
                CreatedOnPeriod = new FacetResult
                {
                    Name = nameof(Job.CreatedOn),
                    Values = createdOnRanges
                        .Select((range, index) => new FacetValueExtended
                        {
                            Range = range.Period,
                            Value = range.DateTime.ToString("o"),
                            Count = facetsLazy.Value.Result[nameof(JobIndex.Result.CreatedOn_DateTime)].Values[index].Count
                        })
                        .Cast<FacetValue>()
                        .ToList()
                },
                Technologies = facetsLazy.Value.Result[nameof(Job.Technologies)],
                Repositories = facetsLazy.Value.Result[nameof(JobIndex.Result.Repository_FullName)],
                Organizations = facetsLazy.Value.Result[nameof(JobIndex.Result.Repository_Organization)]
            }
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

        if (args.SortBy != null)
        {
            if ( args.SortBy == TransactionSortOrder.CreatedAscending )
            {
                query = query.OrderBy(x => x.DateTime);
            }
            else if ( args.SortBy == TransactionSortOrder.CreatedDescending )
            {
                query = query.OrderByDescending(x => x.DateTime);
            }
            else if ( args.SortBy == TransactionSortOrder.AmountAscending )
            {
                query = query.OrderBy(x => x.Amount);
            }
            else if ( args.SortBy == TransactionSortOrder.AmountDescending )
            {
                query = query.OrderByDescending(x => x.Amount);
            }
        }

        var results = await query
            .ProjectInto<TransactionIndex.Result>()
            .Statistics(out var stats)
            .Skip(args.Skip)
            .Take(args.Limit)
            .ToArrayAsync();
        
        return new PagedResult<TransactionIndex.Result>(results, stats.LongTotalResults);
    }

    private async Task<object?> GetJobSubmissionsAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        ulong id = context.GetArgument<ulong>("id");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        return GetJobSubmissionsDetailsAsync(id, session);
    }

    private async Task<JobSubmissionsDetails?> GetJobSubmissionsDetailsAsync(ulong id, IAsyncDocumentSession session)
    {
        var attemptId = JobAttemptedSnapshot.KeyFrom(id);

        var query = await session.LoadAsync<JobAttemptedSnapshot>(attemptId);

        if (query == null) {
            return null;
        }
        var submission = new JobSubmissionsDetails { };

        var commandRequestId = QueueEngineRequestCommand.KeyFrom(id);
        var engine_cmd = await session.LoadAsync<QueueEngineRequestCommand>(commandRequestId);

        if (engine_cmd == null) {
            return submission;
        }

        var attempt = new AttemptStage { };

        submission.Status = SubmissionStatus.Attempting;
        submission.Attempt = new AttemptStage { };

        var commandResponseId = EngineCommandResponse.KeyFrom(attemptId);;
        var engineResponse = await session.LoadAsync<EngineCommandResponse>(commandResponseId);

        if (engineResponse == null)
        {
            return submission;
        }

        submission.Attempt.Results = engineResponse.ExecutionResult;

        if (engineResponse.ExecutionResult.ReturnCode == 0)
        {
            submission.Attempt.Status = StageStatus.Passed;
        }
        else
        {
            submission.Attempt.Status = StageStatus.Failed;

            return submission;
        }


        var solve = new SolveStage { };

        submission.Solve = solve;

        var solveCommandId = SolveJobCommand.KeyFrom(attemptId);
        var solveCommand = await session.LoadAsync<SolveJobCommand>(solveCommandId);

        if (solveCommand == null || solveCommand.ResultHash == null)
        {
            return submission;
        }

        submission.Status = SubmissionStatus.Solved;

        var result = new SolutionResult {
            SolutionId = solveCommand.SolutionId,
            ResultHash = solveCommand.ResultHash
        };

        if (solveCommand.SolutionId == null)
        {
            submission.Solve.Status = StageStatus.Failed;
        }
        else
        {
            submission.Solve.Status = StageStatus.Passed;
        }

        submission.Solve.Results = result;

        return submission;
    }
}
