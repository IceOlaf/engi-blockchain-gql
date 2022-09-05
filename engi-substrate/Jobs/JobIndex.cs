using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Jobs;

public class JobIndex : AbstractMultiMapIndexCreationTask<JobIndex.Result>
{
    public class Result : Job
    {
        public IEnumerable<string> Query { get; set; } = null!;

        public DateTime? CreatedOn_DateTime { get; set; }

        public DateTime? UpdatedOn_DateTime { get; set; }

        public string? Repository_Organization { get; set; }

        public string[] SolvedBy { get; set; } = null!;

        public string[] SolutionIds { get; set; } = null!;
    }

    public JobIndex()
    {
        AddMap<JobSnapshot>(snapshots => from snapshot in snapshots
            select new Result
            {
                JobId = snapshot.JobId,
                Creator = (string)(object)snapshot.Creator,
                Funding = (string)(object)snapshot.Funding,
                Repository = snapshot.Repository,
                Language = snapshot.Language,
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
                    snapshot.JobId.ToString(),
                    snapshot.Name,
                    snapshot.Repository.Url.Replace("https://github.com/", "")
                },
                CreatedOn_DateTime = snapshot.IsCreation ? snapshot.SnapshotOn.DateTime : null,
                UpdatedOn_DateTime = snapshot.SnapshotOn.DateTime,
                Repository_Organization = snapshot.Repository.Organization,
                SolvedBy = new string[0],
                SolutionIds = new string[0],
            });

        AddMap<JobAttemptedSnapshot>(attempts => from attempt in attempts
             select new Result
             {
                 JobId = attempt.JobId,
                 Creator = null!,
                 Funding = null!,
                 Repository = null!,
                 Language = Language.CSharp,
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
                 Repository_Organization = null,
                 SolvedBy = new string[0],
                 SolutionIds = new string[0]
             });

        AddMap<SolutionSnapshot>(solutions => from solution in solutions
              select new Result
              {
                  JobId = solution.JobId,
                  Creator = null!,
                  Funding = null!,
                  Repository = null!,
                  Language = Language.CSharp,
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
                  Repository_Organization = null,
                  SolvedBy = new [] { (string)(object)solution.Author },
                  SolutionIds = new [] { solution.Id }
              });

        Reduce = results => from result in results
            group result by result.JobId
            into g
            let latest = g.OrderByDescending(x => x.UpdatedOn.DateTime).First()
            let first = g.First(x => x.CreatedOn != null)
            let solvedBy = g.SelectMany(x => x.SolvedBy).Distinct().ToArray()
            select new Result
            {
                JobId = g.Key,
                Creator = first.Creator,
                Funding = first.Funding,
                Repository = first.Repository,
                Language = first.Language,
                Name = first.Name,
                Tests = first.Tests,
                Requirements = first.Requirements,
                Solution = latest.Solution,
                AttemptCount = g.Sum(x => x.AttemptCount),
                SolutionUserCount = solvedBy.Length,
                CreatedOn = first.CreatedOn,
                UpdatedOn = latest.UpdatedOn,
                Query = g.SelectMany(x => x.Query).Distinct(),
                CreatedOn_DateTime = first.CreatedOn.DateTime,
                UpdatedOn_DateTime = latest.UpdatedOn.DateTime,
                Repository_Organization = first.Repository_Organization,
                SolvedBy = solvedBy,
                SolutionIds = g.SelectMany(x => x.SolutionIds).Distinct().ToArray()
            };

        Index(x => x.Query, FieldIndexing.Search);
        Suggestion(x => x.Query);

        StoreAllFields(FieldStorage.Yes);
        
        Store(x => x.CreatedOn_DateTime, FieldStorage.No);
        Store(x => x.UpdatedOn_DateTime, FieldStorage.No);
        Store(x => x.Repository_Organization, FieldStorage.No);

        OutputReduceToCollection = "Jobs";
        PatternForOutputReduceToCollectionReferences = result => $"JobReferences/{result.JobId}";
    }

    public static string ReferenceKeyFrom(ulong jobId)
    {
        return $"JobReferences/{jobId.ToString(StorageFormats.UInt64)}";
    }
}