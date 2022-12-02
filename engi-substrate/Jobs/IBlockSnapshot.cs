namespace Engi.Substrate.Jobs;

public interface IBlockSnapshot
{
    BlockReference SnapshotOn { get; }
}