namespace Engi.Substrate.Jobs;

public class JobSubmissionsDetails
{
    public SubmissionStatus Status { get; set; } = SubmissionStatus.AttemptedOnChain;

    public string UserName { get; set; }

    public Address Address { get; set; }

    public string? ProfileImageUrl { get; set; }

    public ulong AttemptId { get; set; }

    public AttemptStage? Attempt { get; set; } = null;

    public SolveStage? Solve { get; set; } = null;

    public JobSubmissionsDetails(string userName, Address address, string imageUrl, ulong attemptId)
    {
        Status = SubmissionStatus.AttemptedOnChain;
        UserName = userName;
        Address = address;
        ProfileImageUrl = imageUrl;
        AttemptId = attemptId;
    }
}
