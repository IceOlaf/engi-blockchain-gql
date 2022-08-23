using System.Numerics;

namespace Engi.Substrate.Jobs;

public enum JobStatus
{
    Funded,
    Attempted,
    Solved
}

public class Job
{ 
    public string Id { get; set; } = null!;

    public ulong JobId { get; set; }

    public string Creator { get; set; } = null!;

    public BigInteger Funding { get; set; }

    public Repository Repository { get; set; } = null!;

    public Language Language { get; set; }
    
    public string Name { get; set; } = null!;

    public Test[] Tests { get; set; } = null!;

    public FilesRequirement Requirements { get; set; } = null!;

    public Solution? Solution { get; set; }

    public ulong AttemptCount { get; set; }

    public DateTime UpdatedOn { get; set; }

    public ulong UpdatedOnBlockNumber { get; set; }

    public JobStatus Status
    {
        get
        {
            if (Solution != null)
            {
                return JobStatus.Solved;
            }

            if (AttemptCount > 0)
            {
                return JobStatus.Attempted;
            }

            return JobStatus.Funded;
        }
    }

    public static string KeyFrom(ulong jobId)
    {
        return $"jobs/{jobId:00000000000000000000}";
    }

    public static Job Parse(ScaleStreamReader reader)
    {
        return new()
        {
            JobId = reader.ReadUInt64(),
            Creator = reader.ReadAddressAsId(),
            Funding = reader.ReadUInt128(),
            Repository = Repository.Parse(reader),
            Language = reader.ReadEnum<Language>(),
            Name = reader.ReadString()!,
            Tests = reader.ReadList(Test.Parse),
            Requirements = FilesRequirement.Parse(reader),
            Solution = reader.ReadOptional(Solution.Parse)
        };
    }

    public static class MetadataKeys
    {
        public const string SentryId = "SentryId";
        public const string SourceBlockNumber = "SourceBlockNumber";
    }
}