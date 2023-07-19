using Engi.Substrate.Github;
using Raven.Client.Documents.Indexes;
using static Engi.Substrate.Jobs.JobIndexUtils;

namespace Engi.Substrate.Jobs;

public class JobIndex : AbstractMultiMapIndexCreationTask<JobIndex.Result>
{
    public class Result : Job
    {
        public IEnumerable<string> Query { get; set; } = null!;

        public DateTime? CreatedOn_DateTime { get; set; }

        public DateTime? UpdatedOn_DateTime { get; set; }

        public string? UpdatedOn_Date { get; set; }

        public string? Repository_FullName { get; set; }

        public string? Repository_Organization { get; set; }

        public string[] SolutionIds { get; set; } = null!;

        public RepositoryComplexity? Complexity { get; set; }
    }

    public JobIndex()
    {
        AddMap<JobSnapshot>(snapshots => from snapshot in snapshots
            let repositoryFullName = snapshot.Repository.Url.Replace("https://github.com/", "")
            let readme = LoadDocument<GithubRepositoryReadme>($"readme/github/{repositoryFullName}")
            select new Result
            {
                JobId = snapshot.JobId,
                Creator = (string)(object)snapshot.Creator,
                Funding = (string)(object)snapshot.Funding,
                Repository = snapshot.Repository,
                Technologies = snapshot.Technologies,
                Name = snapshot.Name,
                Tests = snapshot.Tests,
                Requirements = snapshot.Requirements,
                Solution = snapshot.Solution,
                AttemptCount = 0,
                SolutionUserCount = 0,
                CreatedOn = snapshot.IsCreation ? snapshot.SnapshotOn : null!,
                UpdatedOn = snapshot.SnapshotOn,
                Query = new []
                {
                    snapshot.JobId.ToString().TrimStart('0'),
                    snapshot.Name,
                    repositoryFullName,
                    String.Join(",", snapshot.Technologies.Select(p=>p.ToString()).ToArray()),
                    readme.Content
                },
                CreatedOn_DateTime = snapshot.IsCreation ? snapshot.SnapshotOn.DateTime : null,
                UpdatedOn_DateTime = snapshot.SnapshotOn.DateTime,
                UpdatedOn_Date = snapshot.SnapshotOn.DateTime.ToString("yyyy-MM-dd"),
                Repository_FullName = snapshot.Repository.FullName,
                Repository_Organization = snapshot.Repository.Organization,
                SolvedBy = new string[0],
                SolutionIds = new string[0],
                Status = JobStatus.None,
                Complexity = null
            });

        AddMap<JobAttemptedSnapshot>(attempts => from attempt in attempts
             select new Result
             {
                 JobId = attempt.JobId,
                 Creator = null!,
                 Funding = null!,
                 Repository = null!,
                 Technologies = new Technology[] { Technology.CSharp },
                 Name = null!,
                 Tests = null!,
                 Requirements = null!,
                 Solution = null,
                 AttemptCount = 1,
                 SolutionUserCount = 0,
                 CreatedOn = null!,
                 UpdatedOn = attempt.SnapshotOn,
                 Query = null!,
                 CreatedOn_DateTime = null,
                 UpdatedOn_DateTime = attempt.SnapshotOn.DateTime,
                 UpdatedOn_Date = null,
                 Repository_FullName = null,
                 Repository_Organization = null,
                 SolvedBy = new string[0],
                 SolutionIds = new string[0],
                 Status = JobStatus.None,
                 Complexity = null
             });

        AddMap<SolutionSnapshot>(solutions => from solution in solutions
              select new Result
              {
                  JobId = solution.JobId,
                  Creator = null!,
                  Funding = null!,
                  Repository = null!,
                  Technologies = new Technology[] { Technology.CSharp },
                  Name = null!,
                  Tests = null!,
                  Requirements = null!,
                  Solution = null,
                  AttemptCount = 1,
                  SolutionUserCount = 0,
                  CreatedOn = null!,
                  UpdatedOn = solution.SnapshotOn,
                  Query = null!,
                  CreatedOn_DateTime = null,
                  UpdatedOn_DateTime = solution.SnapshotOn.DateTime,
                  UpdatedOn_Date = solution.SnapshotOn.DateTime.ToString("yyyy-MM-dd"),
                  Repository_FullName = null,
                  Repository_Organization = null,
                  SolvedBy = new [] { (string)(object)solution.Author },
                  SolutionIds = new [] { solution.Id },
                  Status = JobStatus.None,
                  Complexity = null
              });

        AddMap<RepositoryAnalysis>(analyses => from analysis in analyses
              where analysis.ProcessedOn != null
              select new Result
              {
                  JobId = analysis.JobId,
                  Creator = null!,
                  Funding = null!,
                  Repository = null!,
                  Technologies = new Technology[0],
                  Name = null!,
                  Tests = null!,
                  Requirements = null!,
                  Solution = null,
                  AttemptCount = 0,
                  SolutionUserCount = 0,
                  CreatedOn = null!,
                  UpdatedOn = null!,
                  Query = null!,
                  CreatedOn_DateTime = null,
                  UpdatedOn_DateTime = analysis.ProcessedOn,
                  UpdatedOn_Date = null,
                  Repository_FullName = null,
                  Repository_Organization = null,
                  SolvedBy = new string[0],
                  SolutionIds = new string[0],
                  Status = JobStatus.None,
                  Complexity = analysis.Complexity
              });

        Reduce = results => from result in results
            group result by result.JobId
            into g
            let latest = g.Where(x => x.Complexity == null).OrderByDescending(x => x.UpdatedOn.DateTime).First()
            let first = g.First(x => x.CreatedOn != null)
            let solvedBy = g.Where(x => x.Complexity == null).SelectMany(x => x.SolvedBy).Distinct().ToArray()
            let attemptCount = g.Sum(x => x.AttemptCount)
            let analysis = g.First(x => x.Complexity != null)
            select new Result
            {
                JobId = g.Key,
                Creator = first.Creator,
                Funding = first.Funding,
                Repository = first.Repository,
                Technologies = first.Technologies,
                Name = first.Name,
                Tests = first.Tests,
                Requirements = first.Requirements,
                Solution = latest.Solution,
                AttemptCount = attemptCount,
                SolutionUserCount = solvedBy.Length,
                CreatedOn = first.CreatedOn,
                UpdatedOn = latest.UpdatedOn,
                Query = g.SelectMany(x => x.Query).Distinct(),
                CreatedOn_DateTime = first.CreatedOn.DateTime,
                UpdatedOn_DateTime = latest.UpdatedOn.DateTime,
                UpdatedOn_Date = latest.UpdatedOn_Date,
                Repository_FullName = first.Repository_FullName,
                Repository_Organization = first.Repository_Organization,
                SolvedBy = solvedBy,
                SolutionIds = g.SelectMany(x => x.SolutionIds).Distinct().ToArray(),
                Status = CalculateStatus(latest.Solution, attemptCount),
                Complexity = (analysis != null) ? analysis.Complexity : latest.Complexity
            };

        Index(x => x.Technologies, FieldIndexing.Search);
        Index(x => x.Query, FieldIndexing.Search);
        //Index(x => x.Complexity, FieldIndexing.Search);

        Suggestion(x => x.Query);

        StoreAllFields(FieldStorage.Yes);

        Store(x => x.CreatedOn_DateTime, FieldStorage.No);
        Store(x => x.UpdatedOn_DateTime, FieldStorage.No);
        Store(x => x.Repository_FullName, FieldStorage.No);
        Store(x => x.Repository_Organization, FieldStorage.No);

        OutputReduceToCollection = "Jobs";
        PatternForOutputReduceToCollectionReferences = result => $"JobReferences/{result.JobId}";

        AdditionalSources = new Dictionary<string, string>
        {
            {
                "JobIndexUtils",
                @"
using Engi.Substrate.Jobs;

namespace Engi.Substrate.Jobs
{
    public static class JobIndexUtils
    {
        public static string CalculateStatus(Solution solution, int attemptCount)
        {
            if (solution != null)
            {
                return ""Complete"";
            }

            if (attemptCount > 0)
            {
                return ""Active"";
            }

            return ""Open"";
        }
    }
}
"
            }
        };
    }

    public static string ReferenceKeyFrom(ulong jobId)
    {
        return $"JobReferences/{jobId.ToString(StorageFormats.UInt64)}";
    }
}

public static class JobIndexUtils
{
    public static JobStatus CalculateStatus(Solution? solution, int attemptCount) => throw new NotImplementedException();
}
