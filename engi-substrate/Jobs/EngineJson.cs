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
            new TestConverter()
        }
    };

    class TestConverter : JsonConverter<Test>
    {
        public override Test Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

            bool required = json.TryGetProperty("required", out var requiredProp) && requiredProp.GetBoolean();

            return new Test
            {
                Id = json.GetProperty("id").GetString()!,
                Result = result,
                FailedResultMessage = failedResultMessage,
                Required = required
            };
        }

        public override void Write(Utf8JsonWriter writer, Test value, JsonSerializerOptions options) => throw new NotImplementedException();
    }
}
