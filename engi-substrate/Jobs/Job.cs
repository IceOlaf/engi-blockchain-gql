namespace Engi.Substrate.Jobs;

public class Job
{
    public ulong JobId { get; set; }

    public string Creator { get; init; } = null!;

    public string Funding { get; init; } = null!;

    public Repository Repository { get; init; } = null!;

    public Language Language { get; init; }

    public string Name { get; init; } = null!;

    public Test[] Tests { get; init; } = null!;

    public FilesRequirement Requirements { get; init; } = null!;

    public Solution? Solution { get; init; }

    public int AttemptCount { get; init; }

    public BlockReference CreatedOn { get; set; } = null!;

    public BlockReference UpdatedOn { get; set; } = null!;

    public JobStatus Status
    {
        get
        {
            if (Solution != null)
            {
                return JobStatus.Complete;
            }

            if (AttemptCount > 0)
            {
                return JobStatus.Active;
            }

            return JobStatus.Open;
        }
    }
}