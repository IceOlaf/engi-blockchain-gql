using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Jobs;

public class SolutionIndex : AbstractIndexCreationTask<SolutionSnapshot, SolutionIndex.Result>
{
    public class Result
    {
        public ulong JobId { get; set; }

        public ulong SolutionId { get; set; }
    }

    public SolutionIndex()
    {
        Map = solutions => from solution in solutions
            select new Result
            {
                JobId = solution.JobId,
                SolutionId = solution.SolutionId
            };
    }
}