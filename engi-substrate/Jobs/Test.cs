using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public class Test : IScaleSerializable, IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Id { get; init; } = null!;

    public TestResult Result { get; init; }

    public string? FailedResultMessage { get; init; }

    public bool Required { get; set; }

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata _)
    {
        writer.Write(Id);
        writer.Write(Result);
        
        if (Result == TestResult.Failed)
        {                    
            writer.Write(FailedResultMessage!);
        }

        writer.Write(Required);
    }

    public static Test Parse(ScaleStreamReader reader)
    {
        string id = reader.ReadString()!;
        var result = reader.ReadEnum<TestResult>();

        return new()
        {
            Id = id,
            Result = result,
            FailedResultMessage = result != TestResult.Failed ? null : reader.ReadString(),
            Required = reader.ReadBool()
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Result == TestResult.Failed && string.IsNullOrEmpty(FailedResultMessage))
        {
            yield return new ValidationResult("Error message is required for failing tests.",
                new[] { nameof(Result), nameof(FailedResultMessage) });
        }
    }
}