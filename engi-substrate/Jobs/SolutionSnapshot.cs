namespace Engi.Substrate.Jobs;

public class SolutionSnapshot : Solution, IBlockSnapshot
{
    public string Id { get; init; } = null!;

    public BlockReference SnapshotOn { get; set; } = null!;

    public SolutionSnapshot() { }

    public SolutionSnapshot(Solution solution, BlockReference block)
    {
        Id = KeyFrom(solution.SolutionId, block.Number);
        SolutionId = solution.SolutionId;
        JobId = solution.JobId;
        Author = solution.Author;
        PatchUrl = solution.PatchUrl;
        Attempt = solution.Attempt;
        SnapshotOn = block;
    }

    public static string KeyFrom(ulong solutionId, ulong blockNumber)
    {
        return $"SolutionSnapshots/{solutionId.ToString(StorageFormats.UInt64)}/{blockNumber.ToString(StorageFormats.UInt64)}";
    }
}