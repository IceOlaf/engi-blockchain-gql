using Engi.Substrate.Jobs;
using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Server.Indexing;

public class JobIndex : AbstractIndexCreationTask<Job, JobIndex.Result>
{
    public class Result
    {
        public string Creator { get; set; } = null!;

        public JobStatus Status { get; set; }

        public string[] Query { get; set; } = null!;

        public Language Language { get; set; }

        public uint Funding { get; set; }
    }

    public JobIndex()
    {
        Map = jobs => from job in jobs
            select new Result
            {
                Creator = job.Creator,
                Status = job.Status,
                Query = new []
                {
                    job.JobId.ToString(),
                    job.Name,
                    job.Repository.Url.Replace("https://github.com/", "")
                },
                Language = job.Language,
                Funding = uint.Parse((string)(object)job.Funding)
            };

        Index(x => x.Query, FieldIndexing.Search);
    }
}