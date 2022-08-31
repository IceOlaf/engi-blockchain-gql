using System.Numerics;

namespace Engi.Substrate.Jobs;

public class JobSnapshot
{ 
    public string Id { get; init; } = null!;

    public ulong JobId { get; init; }

    public string Creator { get; init; } = null!;

    public BigInteger Funding { get; init; }

    public Repository Repository { get; init; } = null!;

    public Language Language { get; init; }
    
    public string Name { get; init; } = null!;

    public Test[] Tests { get; init; } = null!;

    public FilesRequirement Requirements { get; init; } = null!;

    public Solution? Solution { get; init; }

    public bool IsCreation { get; set; }

    public BlockReference SnapshotOn { get; init; } = null!;

    private JobSnapshot() {}

    public static string KeyFrom(ulong jobId, ulong blockNumber)
    {
        return $"JobSnapshots/{jobId:D20}/{blockNumber:D20}";
    }

    public static JobSnapshot Parse(ScaleStreamReader reader, BlockReference blockReference)
    {
        ulong jobId = reader.ReadUInt64();

        return new()
        {
            Id = KeyFrom(jobId, blockReference.Number),
            JobId = jobId,
            Creator = reader.ReadAddressAsId(),
            Funding = reader.ReadUInt128(),
            Repository = Repository.Parse(reader),
            Language = reader.ReadEnum<Language>(),
            Name = reader.ReadString()!,
            Tests = reader.ReadList(Test.Parse),
            Requirements = FilesRequirement.Parse(reader),
            Solution = reader.ReadOptional(Solution.Parse),

            // non chain

            SnapshotOn = blockReference
        };
    }

   
}