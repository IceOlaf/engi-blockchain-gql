using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Async;

public class DistributeCodeCommand
{
    private DistributeCodeCommand() { }

    public DistributeCodeCommand(JobSnapshot job)
    {
        Id = KeyFrom(job);
        JobSnapshotId = job.Id;
    }

    public string Id { get; private init; } = null!;

    public string JobSnapshotId { get; private init; } = null!;

    public string? PullRequestUrl { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public string? SentryId { get; set; }

    public static string KeyFrom(JobSnapshot job)
    {
        // only one per solution

        return $"DistributeCodeCommands/{job.Solution!.SolutionId.ToString(StorageFormats.UInt64)}";
    }
}
