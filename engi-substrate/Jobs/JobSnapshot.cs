using System.Numerics;

namespace Engi.Substrate.Jobs;

public class JobSnapshot : IBlockSnapshot
{ 
    public string Id { get; init; } = null!;

    public ulong JobId { get; init; }

    public Address Creator { get; init; } = null!;

    public BigInteger Funding { get; init; }

    public Repository Repository { get; init; } = null!;

    public Language Language { get; init; }
    
    public string Name { get; init; } = null!;

    public Test[] Tests { get; init; } = null!;

    public FilesRequirement? Requirements { get; init; }

    public Solution? Solution { get; init; }

    public bool IsCreation { get; set; }

    public BlockReference SnapshotOn { get; init; } = null!;

    internal JobSnapshot() {}

    public static string KeyFrom(ulong jobId, ulong blockNumber)
    {
        return $"JobSnapshots/{jobId.ToString(StorageFormats.UInt64)}/{blockNumber.ToString(StorageFormats.UInt64)}";
    }

    public static JobSnapshot Parse(ScaleStreamReader reader, BlockReference blockReference)
    {
        ulong jobId = reader.ReadUInt64();

        return new()
        {
            Id = KeyFrom(jobId, blockReference.Number),
            JobId = jobId,
            Creator = reader.ReadAddress(),
            Funding = reader.ReadUInt128(),
            Repository = Repository.Parse(reader),
            Language = reader.ReadEnum<Language>(),
            Name = reader.ReadString()!,
            Tests = reader.ReadList(Test.Parse),
            Requirements = reader.ReadOptional(FilesRequirement.Parse),
            Solution = reader.ReadOptional(Solution.Parse),

            // non chain

            SnapshotOn = blockReference
        };
    }
}
