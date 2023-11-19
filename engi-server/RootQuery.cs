using System.Numerics;
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
using GraphQL.Server.Transports.AspNetCore.Errors;
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
            .ResolveAsync(GetJobDraft);

        Field<ListGraphType<JobDraftGraphType>>("drafts")
            .Argument<ListDraftsArgumentsGraphType>("args")
            .ResolveAsync(GetJobDrafts);

        Field<EngineerGraphType>("engineer")
            .Argument<StringGraphType>("id")
            .ResolveAsync(GetEngineer);

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

    private async Task<object?> GetJobDrafts(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();
        var args = context.GetOptionalValidatedArgument<ListDraftsArguments>("args") ?? new();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        if (user == null)
        {
            throw new AccessDeniedError("User not logged in.");
        }

        var userAddress = user!.Address;

        var drafts = await session
            .Query<JobDraft>()
            .Where(x => x.CreatedBy == userAddress)
            .Skip(args.Skip)
            .Take(args.Take)
            .ToListAsync();

        return drafts;
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

    private async Task<object?> GetEngineer(IResolveFieldContext context)
    {
        string id = context.GetValidatedArgument<string>("id", new AccountIdAttribute());

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        var address = Address.Parse(id);
        var balance = new BigInteger(0);

        try
        {
            var info = await substrate.GetSystemAccountAsync(address);
            balance = info == null ? 0 : info.Data.Free;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

        var addressKey = UserAddressReference.KeyFrom(id);
        var addressReference = await session.LoadAsync<UserAddressReference>(addressKey);

        if (addressReference == null)
        {
            return null;
        }

        var user = await session.LoadAsync<User>(addressReference.UserId);

        var creatorAggregatesReference = await session
            .LoadAsync<ReduceOutputReference>(
                JobUserAggregatesIndex.Result.ReferenceKeyFrom(id),
                include => include.IncludeDocuments(x => x.ReduceOutputs));

        var solved = 0;
        var created = 0;

        if (creatorAggregatesReference?.ReduceOutputs.Any() == true)
        {
            var creatorAggregates = await session
                .LoadAsync<JobUserAggregatesIndex.Result>(
                    creatorAggregatesReference.ReduceOutputs.FirstOrDefault()
                );

            solved = creatorAggregates.SolvedCount;
            created = creatorAggregates.CreatedCount;
        }

        var earnings = new EngineerEarnings {
            PastDay = 0,
            PastWeek = 0,
            PastMonth = 0,
            Lifetime = 0,
        };

        var engineer = new Engineer {
            DisplayName = user.Display,
            ProfileImageUrl = user.ProfileImageUrl,
            Email = user.Email,
            UserType = user.UserType,
            Balance = balance,
            BountiesSolved = solved,
            BountiesCreated = created,
            Earnings = earnings,
            Techologies = new Technology[0],
            RepositoriesWorkedOn = new string[0],
            RootOrganization = "",
        };

        return engineer;
    }

    private async Task<object?> GetJobAsync(IResolveFieldContext context)
    {
        ulong jobId = context.GetArgument<ulong>("id");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(GetType());

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
                    var submission = await GetJobSubmissionsDetailsAsync(id, userAddress, session);

                    if (submission != null)
                    {
                        submissions.Add(submission);
                    }
                }

                job.PopulateSubmissions(submissions);
            }
        }
        var solutions = await session.LoadAsync<SolutionSnapshot>(job.SolutionIds);
        job.PopulateSolutions(null, solutions.Values, logger);

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
                    var submission = await GetJobSubmissionsDetailsAsync(id, userAddress, session);

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

        ulong attemptId = context.GetArgument<ulong>("id");

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        string id = JobAttemptedSnapshot.KeyFrom(attemptId);

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
                var submission = await GetJobSubmissionsDetailsAsync(id, null, session);

                if (submission != null)
                {
                    submissions.Add(submission);
                }
            }
            return new PagedResult<JobSubmissionsDetails>(submissions, submissions.Count);
        }

        return new PagedResult<JobSubmissionsDetails>(new JobSubmissionsDetails[] {}, 0);
    }

    private async Task<JobSubmissionsDetails?> GetJobSubmissionsDetailsAsync(string attemptId, Address? filterAddress, IAsyncDocumentSession session)
    {
        var attempt = await session.LoadAsync<JobAttemptedSnapshot>(attemptId);

        if (attempt == null)
        {
            return null;
        }

        if (filterAddress != null && filterAddress != attempt.Attempter)
        {
            return null;
        }

        var addressKey = UserAddressReference.KeyFrom(attempt.Attempter);
        var addressReference = await session.LoadAsync<UserAddressReference>(addressKey);
        var user = await session.LoadAsync<User>(addressReference.UserId);
        var creatorAggregates = await session
            .Query<JobUserAggregatesIndex.Result>()
            .ProjectInto<JobUserAggregatesIndex.Result>()
            .FirstOrDefaultAsync(x => x.UserId == addressReference.UserId);

        UserInfo userInfo = new ()
        {
            Address = attempt.Attempter,
            Display = user.Display,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedOn = user.CreatedOn,
            CreatedJobsCount = creatorAggregates?.CreatedCount ?? 0,
            SolvedJobsCount = creatorAggregates?.SolvedCount ?? 0
        };
        var submission = new JobSubmissionsDetails(userInfo, attempt.AttemptId, attempt.SnapshotOn.DateTime);

        var commandRequestId = QueueEngineRequestCommand.KeyFrom(attempt.AttemptId);
        var engineCmd = await session.LoadAsync<QueueEngineRequestCommand>(commandRequestId);

        if (engineCmd == null)
        {
            return submission;
        }

        submission.Status = SubmissionStatus.EngineAttempting;
        submission.Attempt = new AttemptStage();

        var commandResponseId = EngineCommandResponse.KeyFrom(attemptId);;
        var engineResponse = await session.LoadAsync<EngineCommandResponse>(commandResponseId);

        if (engineResponse == null)
        {
            return submission;
        }

        var rawResult = JsonSerializer.Deserialize<JsonElement>(engineResponse.ExecutionResult.Stdout!);
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

        submission.Solve = new SolveStage();

        var solveCommandId = SolveJobCommand.KeyFrom(attemptId);
        var solveCommand = await session.LoadAsync<SolveJobCommand>(solveCommandId);

        if (solveCommand?.ResultHash == null)
        {
            return submission;
        }

        submission.Status = SubmissionStatus.SolvedOnChain;

        var result = new SolutionResult
        {
            SolutionId = solveCommand.SolutionId,
            ResultHash = solveCommand.ResultHash
        };

        submission.Solve.Status = solveCommand.SolutionId == null ? StageStatus.Failed : StageStatus.Passed;
        submission.Solve.Results = result;

        return submission;
    }
}
