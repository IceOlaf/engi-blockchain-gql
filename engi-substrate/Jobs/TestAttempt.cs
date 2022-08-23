using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Jobs;

public class TestAttempt : IScaleSerializable
{
    [Required]
    public string Id { get; set; } = null!;

    public TestResult Result { get; set; }

    public void Serialize(ScaleStreamWriter writer)
    {
        writer.Write(Id);
        writer.Write(Result);
    }

    public static TestAttempt Parse(ScaleStreamReader reader)
    {
        return new()
        {
            Id = reader.ReadString()!,
            Result = reader.ReadEnum<TestResult>()
        };
    }
}