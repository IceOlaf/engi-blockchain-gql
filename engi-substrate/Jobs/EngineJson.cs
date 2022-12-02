using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engi.Substrate.Jobs;

public static class EngineJson
{
    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, PayloadSerializationOptions)!;
    }

    private static readonly JsonSerializerOptions PayloadSerializationOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TestAttemptConverter()
        }
    };

    class TestAttemptConverter : JsonConverter<TestAttempt>
    {
        public override TestAttempt Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            TestResult result;
            string? failedResultMessage = null;

            var json = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            var resultProp = json.GetProperty("result");

            if (resultProp.ValueKind == JsonValueKind.String)
            {
                result = Enum.Parse<TestResult>(resultProp.GetString()!);

                if (result == TestResult.Failed)
                {
                    throw new InvalidOperationException("Invalid JSON; TestResult.Failed requires the error message.");
                }
            }
            else if (resultProp.ValueKind == JsonValueKind.Object)
            {
                if (!resultProp.TryGetProperty("Failed", out var failedProp))
                {
                    throw new InvalidOperationException("Invalid JSON; TestResult.Failed requires the error message.");
                }

                result = TestResult.Failed;
                failedResultMessage = failedProp.GetString()!;
            }
            else
            {
                throw new InvalidOperationException(
                    "Invalid JSON; TestResult must be an object for TestResult.Failed or a string otherwise.");
            }

            return new TestAttempt
            {
                Id = json.GetProperty("id").GetString()!,
                Result = result,
                FailedResultMessage = failedResultMessage
            };
        }

        public override void Write(Utf8JsonWriter writer, TestAttempt value, JsonSerializerOptions options) => throw new NotImplementedException();
    }
}
