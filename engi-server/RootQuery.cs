using System.Text.Json;
using System.Linq.Expressions;
using Engi.Substrate.Identity;
using Engi.Substrate.Indexing;
using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Async;
using Engi.Substrate.Server.Types;
using Engi.Substrate.Server.Types.Engine;
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

        Field<AnalysisQuery>("analysis")
            .Resolve(_ => new { });

        Field<ActivityGraphType>("activity")
            .Argument<ActivityArgumentsGraphType>("args")
            .ResolveAsync(GetActivityAsync)
            .Description("Get the job activity for the last N days.");

        Field<AuthQuery>("auth")
            .Resolve(_ => new { });

        Field<GithubQuery>("github")
            .Resolve(_ => new { });

        Field<JobDraftGraphType>("draft")
            .Argument<NonNullGraphType<StringGraphType>>("id")
            .Resolve(GetJobDraft);

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

        Field<JobSubmissionsGraphType>("submission")
            .Argument<NonNullGraphType<UInt64GraphType>>("id")
            .ResolveAsync(GetSubmissionAsync);

        Field<JobSubmissionsDetailsPagedResult>("submissions")
            .Argument<JobSubmissionsDetailsPagedQueryArgumentsGraphType>("query")
            .ResolveAsync(GetSubmissionsAsync);
    }

    private async Task<object?> GetJobDraft(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        string id = context.GetArgument<string>("id");

        var draft = await session.LoadAsync<JobDraft>(id);

        return draft;
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

        var job = await session
            .LoadAsync<JobIndex.Result>(reference.ReduceOutputs.First(),
                include => include.IncludeDocuments<JobUserAggregatesIndex.Result>(x => $"JobUserAggregates/{x.Creator}"));

        if (context.User!.Identity!.Name != null)
        {
            var user = await session.LoadAsync<User>(context.User!.Identity!.Name);
            var userAddress = user!.Address;

            if (job.AttemptIds.Length > 0 && userAddress != null)
            {
                List<JobSubmissionsDetails> submissions = new List<JobSubmissionsDetails>();
                foreach (var id in job.AttemptIds)
                {
                    var submission = await GetJobSubmissionsDetailsAsync(Convert.ToUInt64(id, 10), userAddress, session);

                    if (submission != null)
                    {
                        submissions.Add(submission);
                    }
                }

                job.PopulateSubmissions(submissions);
            }
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
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(GetType());

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
            .Include(x => x.Complexity)
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

        await session.Advanced.Eagerly
            .ExecuteAllPendingLazyOperationsAsync();

        var solutionsByJobId = resultsLazy.Value.Result
            .ToDictionary(x => x.JobId, x => session.LoadAsync<SolutionSnapshot>(x.SolutionIds).Result.Values);

        if (context.User!.Identity!.Name != null)
        {
            Dictionary<ulong, List<JobSubmissionsDetails>> submissionsByJobId = new Dictionary<ulong, List<JobSubmissionsDetails>>();

            var user = await session.LoadAsync<User>(context.User!.Identity!.Name);
            var userAddress = user!.Address;

            foreach (var job in resultsLazy.Value.Result)
            {
                List<JobSubmissionsDetails> submissions = new List<JobSubmissionsDetails>();

                foreach (var id in job.AttemptIds)
                {
                    var submission = await GetJobSubmissionsDetailsAsync(Convert.ToUInt64(id, 10), userAddress, session);

                    if (submission != null)
                    {
                        submissions.Add(submission);
                    }
                }

                submissionsByJobId[job.JobId] = submissions;
            }

            foreach (var job in resultsLazy.Value.Result)
            {
                var submissions = submissionsByJobId[job.JobId];
                job.PopulateSubmissions(submissions);
            }
        }

        foreach (var job in resultsLazy.Value.Result)
        {
            var solutions = solutionsByJobId[job.JobId];
            job.PopulateSolutions(null, solutions, logger);
        }

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

    private async Task<object?> GetSubmissionAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        ulong id = context.GetArgument<ulong>("id");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        return await GetJobSubmissionsDetailsAsync(id, null, session);
    }

    private async Task<object?> GetSubmissionsAsync(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        var args = context.GetValidatedArgument<JobSubmissionsDetailsPagedQueryArguments>("query");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var reference = await session
            .LoadAsync<ReduceOutputReference>(JobIndex.ReferenceKeyFrom(args.JobId),
                include => include.IncludeDocuments(x => x.ReduceOutputs));

        if (reference == null)
        {
            return null;
        }

        var job = await session
            .LoadAsync<JobIndex.Result>(reference.ReduceOutputs.First(),
                include => include.IncludeDocuments<JobUserAggregatesIndex.Result>(x => $"JobUserAggregates/{x.Creator}"));

        if (job.AttemptIds.Length > 0)
        {
            List<JobSubmissionsDetails> submissions = new List<JobSubmissionsDetails>();
            foreach (var id in job.AttemptIds)
            {
                var submission = await GetJobSubmissionsDetailsAsync(Convert.ToUInt64(id, 10), null, session);
                if (submission != null)
                {
                    submissions.Add(submission);
                }
            }
            return new PagedResult<JobSubmissionsDetails>(submissions, submissions.Count);
        }

        return new PagedResult<JobSubmissionsDetails>(new JobSubmissionsDetails[] {}, 0);
    }

    private async Task<JobSubmissionsDetails?> GetJobSubmissionsDetailsAsync(ulong id, Address? filterAddress, IAsyncDocumentSession session)
    {
        var attemptId = JobAttemptedSnapshot.KeyFrom(id);

        var query = await session.LoadAsync<JobAttemptedSnapshot>(attemptId);

        if (query == null) {
            return null;
        }

        if (filterAddress != null && filterAddress != query.Attempter) {
            return null;
        }

        var addressKey = UserAddressReference.KeyFrom(query.Attempter);
        var addressReference = await session.LoadAsync<UserAddressReference>(addressKey);
        var user = await session.LoadAsync<User>(addressReference.UserId);
        var creatorAggregates = await session
                .Query<JobUserAggregatesIndex.Result>()
                .Where(x => x.UserId == addressReference.UserId)
                .FirstOrDefaultAsync();


        UserInfo userInfo = new () {
            Address = query.Attempter,
            Display = user.Display,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedOn = user.CreatedOn,
            CreatedJobsCount = creatorAggregates?.CreatedCount ?? 0,
            SolvedJobsCount = creatorAggregates?.SolvedCount ?? 0
        };
        var submission = new JobSubmissionsDetails(userInfo, id, query.SnapshotOn.DateTime);

        var commandRequestId = QueueEngineRequestCommand.KeyFrom(id);
        var engine_cmd = await session.LoadAsync<QueueEngineRequestCommand>(commandRequestId);

        if (engine_cmd == null) {
            return submission;
        }

        var attempt = new AttemptStage { };

        submission.Status = SubmissionStatus.EngineAttempting;
        submission.Attempt = new AttemptStage { };

        var commandResponseId = EngineCommandResponse.KeyFrom(attemptId);;
        var engineResponse = await session.LoadAsync<EngineCommandResponse>(commandResponseId);

        if (engineResponse == null)
        {
            return submission;
        }

        var rawResult = JsonSerializer.Deserialize<JsonElement>(engineResponse.ExecutionResult.Stdout);
        var attemptJson = rawResult.GetProperty("attempt");
        var testAttempts = EngineJson.Deserialize<EngineAttemptResult>(attemptJson).Tests;

        submission.Attempt.Results = engineResponse.ExecutionResult;
        submission.Attempt.Tests = testAttempts;

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

        submission.Status = SubmissionStatus.SolvedOnChain;

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
