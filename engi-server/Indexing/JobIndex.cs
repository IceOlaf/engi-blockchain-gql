using Engi.Substrate.Jobs;
using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Server.Indexing;

public class JobIndex : AbstractIndexCreationTask<Job, JobIndex.Result>
{
    public class Result
    {
        public string[] Query { get; set; } = null!;
    }

    public JobIndex()
    {
        Map = jobs => from job in jobs
            select new Result
            {
                Query = new []
                {
                    job.JobId.ToString(),
                    job.Name,
                    job.Repository.Url.Replace("https://github.com/", "")
                }
            };

        Index(x => x.Query, FieldIndexing.Search);
    }
}