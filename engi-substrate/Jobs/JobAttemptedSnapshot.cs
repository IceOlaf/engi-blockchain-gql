using System.Text;

namespace Engi.Substrate.Jobs;

public class JobAttemptedSnapshot : IBlockSnapshot, IDispatched
{
    public string Id { get; init; } = null!;

    public ulong AttemptId { get; set; }

    public ulong JobId { get; set; }

    public string Attempter { get; set; } = null!;

    public string PatchFileUrl { get; set; } = null!;

    public BlockReference SnapshotOn { get; init; } = null!;

    public DateTime? DispatchedOn { get; set; }

    public static string KeyFrom(ulong id)
    {
        return $"JobAttemptedSnapshots/{id.ToString(StorageFormats.UInt64)}";
    }

    public static JobAttemptedSnapshot From(
        Dictionary<string, object> arguments,
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
            PatchFileUrl = Encoding.UTF8.GetString(Hex.GetBytes((string) arguments["submission_patch_file_url"])).Trim(),
            SnapshotOn = reference
        };
    }
}
