using Engi.Substrate.Jobs;
using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Server.Indexing;

public class JobIndex : AbstractIndexCreationTask<JobSnapshot, JobIndex.Result>
{
    public class Result : Job
    {
        public IEnumerable<string> Query { get; set; } = null!;

        public DateTime? CreatedOn_DateTime { get; set; }
    }

    public JobIndex()
    {
        Map = snapshots => from snapshot in snapshots
            select new Result
            {
                JobId = snapshot.JobId,
                Creator = snapshot.Creator,
                Funding = snapshot.Funding,
                Repository = snapshot.Repository,
                Language = snapshot.Language,
                Name = snapshot.Name,
                Tests = snapshot.Tests,
                Requirements = snapshot.Requirements,
                Solution = snapshot.Solution,
                AttemptCount = snapshot.AttemptCount,
                CreatedOn = snapshot.IsCreation ? snapshot.SnapshotOn : null!,
                UpdatedOn = snapshot.SnapshotOn,
                Query = new []
                {
                    snapshot.JobId.ToString(),
                    snapshot.Name,
                    snapshot.Repository.Url.Replace("https://github.com/", "")
                },
                CreatedOn_DateTime = snapshot.IsCreation ? snapshot.SnapshotOn.DateTime : null
            };

        Reduce = results => from result in results
            group result by result.JobId
            into g
            let latest = g.OrderByDescending(x => x.UpdatedOn.DateTime).First()
            let createdOn = g.First(x => x.CreatedOn != null).CreatedOn
                            select new Result
            {
                JobId = g.Key,
                Creator = latest.Creator,
                Funding = latest.Funding,
                Repository = latest.Repository,
                Language = latest.Language,
                Name = latest.Name,
                Tests = latest.Tests,
                Requirements = latest.Requirements,
                Solution = latest.Solution,
                AttemptCount = latest.AttemptCount,
                CreatedOn = createdOn,
                UpdatedOn = latest.UpdatedOn,
                Query = g.SelectMany(x => x.Query).Distinct(),
                CreatedOn_DateTime = createdOn.DateTime
            };

        Index(x => x.Query, FieldIndexing.Search);

        StoreAllFields(FieldStorage.Yes);
        Store(x => x.CreatedOn_DateTime, FieldStorage.No);

        OutputReduceToCollection = "Jobs";
        PatternForOutputReduceToCollectionReferences = result => $"JobReferences/{result.JobId}";
    }

    public static string ReferenceKeyFrom(ulong jobId)
    {
        return $"JobReferences/{jobId}";
    }
}