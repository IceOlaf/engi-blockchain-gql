using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Jobs;

public class Test : IScaleSerializable
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Id { get; init; } = null!;

    public TestResult Result { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(1000)]
    public string ResultMessage { get; init; } = string.Empty;

    public TestResult Required { get; init; }

    [Required(AllowEmptyStrings = true), MaxLength(1000)]
    public string RequiredMessage { get; init; } = string.Empty;

    public void Serialize(ScaleStreamWriter writer)
    {
        writer.Write(Id);
        writer.Write(Result);
        writer.Write(ResultMessage);
        writer.Write(Required);
        writer.Write(RequiredMessage);
    }

    public static Test Parse(ScaleStreamReader reader)
    {
        return new()
        {
            Id = reader.ReadString()!,
            Result = reader.ReadEnum<TestResult>(),
            ResultMessage = reader.ReadString()!,
            Required = reader.ReadEnum<TestResult>(),
            RequiredMessage = reader.ReadString()!
        };
    }
}