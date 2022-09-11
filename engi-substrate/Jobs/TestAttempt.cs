using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public class TestAttempt : IScaleSerializable
{
    [Required]
    public string Id { get; set; } = null!;

    public TestResult Result { get; set; }

    public string? FailedResultMessage { get; set; }

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata _)
    {
        writer.Write(Id);
        writer.Write(Result);
        
        if (Result == TestResult.Failed)
        {
            writer.Write(FailedResultMessage ?? string.Empty);
        }
    }

    public static TestAttempt Parse(ScaleStreamReader reader)
    {
        var attempt = new TestAttempt
        {
            Id = reader.ReadString()!
        };

        var testResult = reader.ReadEnum<TestResult>();
        
        attempt.Result = testResult;

        if (testResult == TestResult.Failed)
        {
            attempt.FailedResultMessage = reader.ReadString();
        }
        
        return attempt;
    }
}