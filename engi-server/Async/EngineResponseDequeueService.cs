using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SQS;
using Amazon.SQS.Model;
using Engi.Substrate.Jobs;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Exceptions;
using Sentry;

namespace Engi.Substrate.Server.Async;

public class EngineResponseDequeueService : BackgroundService
{
    private readonly IDocumentStore store;
    private readonly IHub sentry;
    private readonly EngiOptions options;

    private static readonly JsonSerializerOptions MessageSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions PayloadSerializationOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TestConverter()
        }
    };

    public EngineResponseDequeueService(
        IDocumentStore store,
        IHub sentry,
        IOptions<EngiOptions> options)
    {
        this.store = store;
        this.sentry = sentry;
        this.options = options.Value;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sqs = new AmazonSQSClient();

        while (!stoppingToken.IsCancellationRequested)
        {
            ReceiveMessageResponse batch;

            try
            {
                batch = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = options.EngineOutputQueueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20 // max, apparently
                }, stoppingToken);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken == stoppingToken)
                {
                    return;
                }

                sentry.CaptureException(ex);

                throw;
            }

            if (!batch.Messages.Any())
            {
                continue;
            }

            foreach (var item in batch.Messages)
            {
                try
                {
                    await ProcessMessageAsync(item);

                    await sqs.DeleteMessageAsync(new()
                    {
                        QueueUrl = options.EngineOutputQueueUrl,
                        ReceiptHandle = item.ReceiptHandle
                    });
                }
                catch (ConcurrencyException)
                {
                    // someone else was modifying the analysis
                    // ignore, so we can reprocess
                }
                catch (Exception ex)
                {
                    sentry.CaptureException(ex);
                }
            }
        }
    }

    private async Task ProcessMessageAsync(Message item)
    {
        using var session = store.OpenAsyncSession();

        session.Advanced.UseOptimisticConcurrency = true;

        var snsMessage = JsonSerializer.Deserialize<JsonElement>(item.Body);
        string snsMessageBody = snsMessage.GetProperty("Message").GetString()!;

        var executionResult = JsonSerializer
            .Deserialize<CommandLineExecutionResult>(snsMessageBody, MessageSerializationOptions)!;

        var analysis = await session
            .LoadAsync<RepositoryAnalysis>(executionResult.Identifier);

        if (analysis == null)
        {
            throw new InvalidOperationException(
                $"Analysis with identifier={executionResult.Identifier} was not found");
        }

        analysis.ExecutionResult = executionResult;

        analysis.Status = analysis.ExecutionResult.ReturnCode == 0
            ? RepositoryAnalysisStatus.Completed
            : RepositoryAnalysisStatus.Failed;

        if (analysis.Status == RepositoryAnalysisStatus.Completed)
        {
            try
            {
                var payload = JsonSerializer
                    .Deserialize<AnalysisPayload>(executionResult.Stdout, PayloadSerializationOptions)!;

                analysis.Language = payload.Language;
                analysis.Files = payload.Files;
                analysis.Complexity = payload.Complexity;
                analysis.Tests = payload.Tests;
            }
            catch (Exception ex)
            {
                sentry.CaptureException(ex, new()
                {
                    ["messageId"] = item.MessageId
                });
            }
        }

        await session.SaveChangesAsync();
    }

    class AnalysisPayload
    {
        public Language Language { get; set; }

        public string[]? Files { get; set; }

        public RepositoryComplexity? Complexity { get; set; }

        public TestAttempt[]? Tests { get; set; }
    }

    class TestConverter : JsonConverter<Test>
    {
        public override Test? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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