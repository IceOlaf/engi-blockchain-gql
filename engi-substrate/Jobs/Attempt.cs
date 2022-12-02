using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public class Attempt : IScaleSerializable
{
    [Range(1, ulong.MaxValue)]
    public ulong AttemptId { get; set; }

    [Required]
    public Address Attempter { get; set; } = null!;

    [NotNullOrEmptyCollection]
    public TestAttempt[] Tests { get; set; } = null!;

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata meta)
    {
        writer.Write(AttemptId);
        writer.Write(Attempter, meta);
        writer.Write(Tests, meta);
    }

    public static Attempt Parse(ScaleStreamReader reader)
    {
        return new()
        {
            AttemptId = reader.ReadUInt64(),
            Attempter = reader.ReadAddress(),
            Tests = reader.ReadList(TestAttempt.Parse)
        };
    }
}
