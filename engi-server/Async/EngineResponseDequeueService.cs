using System.Text.Json;
using Amazon.Runtime;
using Amazon.SecurityToken;
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
        var stsConfig = new AmazonSecurityTokenServiceConfig();

        if(awsOptions.ServiceUrl != null)
        {
            config.ServiceURL = awsOptions.ServiceUrl;
            stsConfig.ServiceURL = awsOptions.ServiceUrl;
        }

        var sts = new AmazonSecurityTokenServiceClient(stsConfig);

        // TODO: aws account from engiOptions.
        var roleArn = string.Format("arn:aws:iam::{0}:role/{1}", "163803973373", engiOptions.AssumeRole);
        logger.LogInformation("Assuming role with arn: {}", roleArn);

        await sts.AssumeRoleAsync(new AssumeRoleRequest {
            DurationSeconds = 1600,
            RoleSessionName = "EngineSession",
            RoleArn = roleArn,
        }, stoppingToken);

        var credentials = string.IsNullOrEmpty(engiOptions.AssumeRole)
            ? FallbackCredentialsFactory.GetCredentials()
            : new InstanceProfileAWSCredentials(engiOptions.AssumeRole);

        var sqs = new AmazonSQSClient(credentials, config);

        if (string.IsNullOrEmpty(engiOptions.AssumeRole)) {
            logger.LogInformation("Assume role is empty, using fallback");
        } else {
            logger.LogInformation("Using assume role role={role}", engiOptions.AssumeRole);
        }
        logger.LogInformation("Processing queue messages from engine. queue={queue}", engiOptions.EngineOutputQueueUrl);
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
            catch (Exception ex)
            {
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
}
