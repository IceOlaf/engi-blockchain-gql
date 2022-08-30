using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Jobs;

public class Test : IScaleSerializable
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Id { get; init; } = null!;

    public TestResult Result { get; init; }

    public bool Required { get; set; }

    public void Serialize(ScaleStreamWriter writer)
    {
        writer.Write(Id);
        writer.Write(Result);
        writer.Write(Required);
    }

    public static Test Parse(ScaleStreamReader reader)
    {
        return new()
        {
            Id = reader.ReadString()!,
            Result = reader.ReadEnum<TestResult>(),
            Required = reader.ReadBool()
        };
    }
}