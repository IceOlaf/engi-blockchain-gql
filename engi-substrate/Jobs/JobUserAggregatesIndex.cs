using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Jobs;

public class JobUserAggregatesIndex : AbstractIndexCreationTask<Job, JobUserAggregatesIndex.Result>
{
    public class Result
    {
        public string Address { get; set; } = null!;

        public int CreatedCount { get; set; }

        public int SolvedCount { get; set; }

        public static string ReferenceKeyFrom(string address)
        {
            return $"{CollectionName}/{address}";
        }

        public const string CollectionName = "JobUserAggregates";
    }

    public JobUserAggregatesIndex()
    {
        Map = jobs => jobs
            .SelectMany(job => new[] { job.Creator }.Concat(job.SolvedBy).Distinct().Select(address => new Result
            {
                Address = address,
                CreatedCount = job.Creator == address ? 1 : 0,
                SolvedCount = job.SolvedBy.Contains(address) ? 1 : 0
            }));

        Reduce = results => from result in results
            group result by result.Address
            into g
            select new Result
            {
                Address = g.Key,
                CreatedCount = g.Sum(x => x.CreatedCount),
                SolvedCount = g.Sum(x => x.SolvedCount)
            };

        OutputReduceToCollection = Result.CollectionName;
        PatternForOutputReduceToCollectionReferences = result => $"JobUserAggregates/{result.Address}";
    }
}
