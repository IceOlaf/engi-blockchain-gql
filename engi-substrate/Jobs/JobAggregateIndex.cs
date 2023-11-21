using Raven.Client.Documents.Indexes;

using static Engi.Substrate.Jobs.JobAggregateIndexUtils;

namespace Engi.Substrate.Jobs;

public class JobAggregateIndex : AbstractIndexCreationTask<Job, JobAggregateIndex.Result>
{
    public class Result
    {
        public int ActiveJobCount { get; set; }

        public string TotalAmountFunded { get; set; } = null!;

        public IEnumerable<Technology> Technologies { get; set; } = null!;

        public int TechnologyCount { get; set; }
    }

    public JobAggregateIndex()
    {
        Map = jobs => from job in jobs
            select new Result
            {
                ActiveJobCount = job.Status == JobStatus.Open || job.Status == JobStatus.Active ? 1 : 0,
                TotalAmountFunded = job.Funding,
                Technologies = job.Technologies,
                TechnologyCount = 0
            };

        Reduce = results => from result in results
            group result by true into g
            select new Result
            {
                ActiveJobCount = g.Sum(x => x.ActiveJobCount),
                TechnologyCount = g.Select(x => x.Technologies).Distinct().Count(),
                Technologies = g.SelectMany(x => x.Technologies).Distinct(),
                TotalAmountFunded = Sum(g.Select(x => x.TotalAmountFunded))
            };

        AdditionalSources = new Dictionary<string, string>
        {
            {
                "JobAggregateIndexUtils",
@"
using System.Numerics;

using static Engi.Substrate.Jobs.JobAggregateIndexUtils;

namespace Engi.Substrate.Jobs
{
    public static class JobAggregateIndexUtils
    {
        public static string Sum(IEnumerable<string> amounts)
        {
            var sum = BigInteger.Zero;

            foreach (var amount in amounts)
            {
                sum += BigInteger.Parse(amount);
            }

            return sum.ToString(""D40"");
        }
    }
}
"
            }
        };

        StoreAllFields(FieldStorage.Yes);
    }
}

public static class JobAggregateIndexUtils
{
    public static string Sum(IEnumerable<string> amounts) => throw new NotImplementedException();
}
