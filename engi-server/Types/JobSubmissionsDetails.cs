using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class JobSubmissionsDetails
{
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public AttemptStage? Attempt { get; set; } = null;

    public SolveStage? Solve { get; set; } = null;
}
