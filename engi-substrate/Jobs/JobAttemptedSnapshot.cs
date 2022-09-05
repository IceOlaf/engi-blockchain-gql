namespace Engi.Substrate.Jobs;

public class JobAttemptedSnapshot : IBlockSnapshot
{
    public string Id { get; init; } = null!;

    public ulong AttemptId { get; set; }

    public ulong JobId { get; set; }

    public string Attempter { get; set; } = null!;

    public BlockReference SnapshotOn { get; init; } = null!;

    public static JobAttemptedSnapshot From(
        Dictionary<int, object> eventData,
        BlockReference reference)
    {
        ulong attemptId = (ulong) eventData[0];

        return new()
        {
            Id = $"JobAttemptedSnapshots/{attemptId.ToString(StorageFormats.UInt64)}",
            AttemptId = attemptId,
            JobId = (ulong) eventData[1],
            Attempter = (string) eventData[2],
            SnapshotOn = reference
        };
    }
}