using System.Text.Json;
using Amazon.Runtime;
using Amazon.SecurityToken.Model;
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
    private readonly Func<Task<AWSCredentials>> credentialsFactory;
    private readonly ILogger logger;
    private readonly AwsOptions awsOptions;
    private readonly EngiOptions engiOptions;

    public EngineResponseDequeueService(
        IDocumentStore store,
        Func<Task<AWSCredentials>> credentialsFactory,
        ILogger<EngineResponseDequeueService> logger,
        IOptions<AwsOptions> awsOptions,
        IOptions<EngiOptions> engiOptions)
    {
        this.store = store;
        this.credentialsFactory = credentialsFactory;
        this.logger = logger;
        this.awsOptions = awsOptions.Value;
        this.engiOptions = engiOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var credentials = await credentialsFactory();

            var sqs = new AmazonSQSClient(credentials,
                new AmazonSQSConfig().Apply(awsOptions));

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
            catch (Exception ex)
            {
                if (ex is AmazonSQSException sqsException && sqsException.Message.Contains("expired"))
                {
                    logger.LogDebug("SQS client credentials expired unexpectedly; expiration={}",
                        credentials is Credentials withExp ? withExp.Expiration : null);
                }

                logger.LogCritical(ex, "Stopping engine response dequeueing service due to a failure.");
                return;
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
                    logger.LogInformation("Processing queue message from engine.");
                    logger.LogTrace("Processing message={message} queue={queue}", message.Body, engiOptions.EngineOutputQueueUrl);

                    // deserialize message, nested in SNS wrapper

                    var snsMessage = JsonSerializer.Deserialize<JsonElement>(message.Body);

                    string snsMessageBody = snsMessage.GetProperty("Message").GetString()!;

                    var executionResult = JsonSerializer
                        .Deserialize<CommandLineExecutionResult>(snsMessageBody, MessageSerializationOptions)!;

                    await session.StoreAsync(new EngineCommandResponse {
                        Id = EngineCommandResponse.KeyFrom(executionResult.Identifier),
                        ExecutionResult = executionResult
                    });

                    // load the object referenced by the identifier and see what it is

                    var identifiedObject = await session
                        .LoadAsync<object>(executionResult.Identifier);

                    if (identifiedObject == null)
                    {
                        throw new InvalidOperationException(
                            $"Identified object with identifier={executionResult.Identifier} was not found on the return path from the engine.");
                    }

                    if(identifiedObject is JobDraft draft)
                    {
                        var analysis = await session.LoadAsync<RepositoryAnalysis>(draft.AnalysisId);
                        draft.Completed = true;

                        ProcessAnalysis(analysis, executionResult);
                    }
                    else if(identifiedObject is RepositoryAnalysis analysis)
                    {
                        ProcessAnalysis(analysis, executionResult);
                    }
                    else if(identifiedObject is JobAttemptedSnapshot attemptSnapshot)
                    {
                        if(executionResult.ReturnCode != 0)
                        {
                            throw new InvalidOperationException("Engine returned non-zero return code, not sure how to proceed.");
                        }

                        var rawResult = JsonSerializer.Deserialize<JsonElement>(executionResult.Stdout);
                        var attempt = rawResult.GetProperty("attempt");

                        await session.StoreAsync(new SolveJobCommand
                        {
                            Id = SolveJobCommand.KeyFrom(attemptSnapshot.Id),
                            JobAttemptedSnapshotId = attemptSnapshot.Id,
                            EngineResult = EngineJson.Deserialize<EngineAttemptResult>(attempt)
                        });
                    }
                    else
                    {
                        throw new NotImplementedException(
                            $"Identified object is not something we know how to process; type={identifiedObject.GetType()}.");
                    }

                    // save changes

                    await session.SaveChangesAsync();
                }
                catch (ConcurrencyException)
                {
                    // someone else was modifying the analysis
                    // ignore, so we can reprocess
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Processing response from engine failed.");
                }

                // finally delete

                try
                {
                    await sqs.DeleteMessageAsync(new()
                    {
                        QueueUrl = engiOptions.EngineOutputQueueUrl,
                        ReceiptHandle = message.ReceiptHandle
                    });
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
        CommandLineExecutionResult executionResult)
    {
        analysis.ExecutionResult = executionResult;

        analysis.Status = analysis.ExecutionResult.ReturnCode == 0
            ? RepositoryAnalysisStatus.Completed
            : RepositoryAnalysisStatus.Failed;

        if (analysis.Status == RepositoryAnalysisStatus.Completed)
        {
            var result = EngineJson.Deserialize<EngineAnalysisResult>(executionResult.Stdout);

            analysis.Technologies = result.Technologies;
            analysis.Files = result.Files;
            analysis.Complexity = result.Complexity;
            analysis.Tests = result.Tests;
        }

        analysis.ProcessedOn = DateTime.UtcNow;
    }

    private static readonly JsonSerializerOptions MessageSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
