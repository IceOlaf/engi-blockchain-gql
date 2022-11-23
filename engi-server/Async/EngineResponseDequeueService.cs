using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Engi.Substrate.Jobs;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Exceptions;

namespace Engi.Substrate.Server.Async;

public class EngineResponseDequeueService : BackgroundService
{
    private readonly IDocumentStore store;
    private readonly ILogger logger;
    private readonly AwsOptions awsOptions;
    private readonly EngiOptions engiOptions;

    private static readonly JsonSerializerOptions MessageSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EngineResponseDequeueService(
        IDocumentStore store,
        ILogger<EngineResponseDequeueService> logger,
        IOptions<AwsOptions> awsOptions,
        IOptions<EngiOptions> engiOptions)
    {
        this.store = store;
        this.logger = logger;
        this.awsOptions = awsOptions.Value;
        this.engiOptions = engiOptions.Value;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new AmazonSQSConfig();

        if(awsOptions.ServiceUrl != null)
        {
            config.ServiceURL = awsOptions.ServiceUrl;
        }

        var sqs = new AmazonSQSClient(config);

        while (!stoppingToken.IsCancellationRequested)
        {
            ReceiveMessageResponse batch;

            try
            {
                batch = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = engiOptions.EngineOutputQueueUrl,
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

                logger.LogError(ex, "Operation was cancelled unexpectedly.");

                throw;
            }

            if (!batch.Messages.Any())
            {
                continue;
            }

            foreach (var message in batch.Messages)
            {
                using var session = store.OpenAsyncSession();

                session.Advanced.UseOptimisticConcurrency = true;

                try
                {
                    // deserialize message, nested in SNS wrapper

                    var snsMessage = JsonSerializer.Deserialize<JsonElement>(message.Body);

                    string snsMessageBody = snsMessage.GetProperty("Message").GetString()!;

                    var executionResult = JsonSerializer
                        .Deserialize<CommandLineExecutionResult>(snsMessageBody, MessageSerializationOptions)!;

                    // load the object referenced by the identifier and see what it is

                    var identifiedObject = await session
                        .LoadAsync<object>(executionResult.Identifier);

                    if (identifiedObject == null)
                    {
                        throw new InvalidOperationException(
                            $"Identified object with identifier={executionResult.Identifier} was not found on the return path from the engine.");
                    }

                    if(identifiedObject is RepositoryAnalysis analysis)
                    {
                        ProcessAnalysis(analysis, executionResult, message);
                    }
                    else if(identifiedObject is JobAttemptedSnapshot attempt)
                    {
                        if(executionResult.ReturnCode != 0)
                        {
                            throw new InvalidOperationException("Engine returned non-zero return code, not sure how to proceed.");
                        }

                        await session.StoreAsync(new SolveJobCommand
                        {
                            JobAttemptedSnapshotId = attempt.Id,
                            EngineResult = EngineExecutionResult.Deserialize(executionResult.Stdout)
                        });
                    }
                    else
                    {
                        throw new NotImplementedException(
                            $"Identified object is not something we know how to process; type={identifiedObject.GetType()}.");
                    }

                    // save changes and delete

                    await session.SaveChangesAsync();

                    await sqs.DeleteMessageAsync(new()
                    {
                        QueueUrl = engiOptions.EngineOutputQueueUrl,
                        ReceiptHandle = message.ReceiptHandle
                    });
                }
                catch (ConcurrencyException)
                {
                    // someone else was modifying the analysis
                    // ignore, so we can reprocess
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Deleting message from queue failed.");
                }
            }
        }
    }

    private void ProcessAnalysis(
        RepositoryAnalysis analysis,
        CommandLineExecutionResult executionResult,
        Message message)
    {
        analysis.ExecutionResult = executionResult;

        analysis.Status = analysis.ExecutionResult.ReturnCode == 0
            ? RepositoryAnalysisStatus.Completed
            : RepositoryAnalysisStatus.Failed;

        if (analysis.Status == RepositoryAnalysisStatus.Completed)
        {
            var result = EngineExecutionResult.Deserialize(executionResult.Stdout);

            analysis.Language = result.Language;
            analysis.Files = result.Files;
            analysis.Complexity = result.Complexity;
            analysis.Tests = result.Tests;
        }

        analysis.ProcessedOn = DateTime.UtcNow;
    }
}
