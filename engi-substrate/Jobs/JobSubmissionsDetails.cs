namespace Engi.Substrate.Jobs;

public class JobSubmissionsDetails
{
    public SubmissionStatus Status { get; set; } = SubmissionStatus.AttemptedOnChain;

    public DateTime AttemptCreated { get; set; }

    public UserInfo UserInfo { get; set; }

    public ulong AttemptId { get; set; }

    public AttemptStage? Attempt { get; set; } = null;

    public SolveStage? Solve { get; set; } = null;

    public JobSubmissionsDetails(UserInfo userInfo, ulong attemptId, DateTime createdOn)
    {
        Status = SubmissionStatus.AttemptedOnChain;
        UserInfo = userInfo;
        AttemptId = attemptId;
        AttemptCreated = createdOn;
    }
}
