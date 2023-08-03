namespace Engi.Substrate.Jobs;

public class JobSubmissionsDetails
{
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public ulong AttemptId { get; set; }

    public AttemptStage? Attempt { get; set; } = null;

    public SolveStage? Solve { get; set; } = null;
}
