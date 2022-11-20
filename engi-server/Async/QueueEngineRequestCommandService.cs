using System.Text.Json;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Sentry;

namespace Engi.Substrate.Server.Async;

public class QueueEngineRequestCommandService : SubscriptionProcessingBase<QueueEngineRequestCommand>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AwsOptions awsOptions;
    private readonly EngiOptions engiOptions;

    public QueueEngineRequestCommandService(
        IDocumentStore store, 
        IServiceProvider serviceProvider,
        IWebHostEnvironment env, 
        IHub sentry,
        IOptions<AwsOptions> awsOptions,
        IOptions<EngiOptions> engiOptions,
        ILoggerFactory loggerFactory) 
        : base(store, serviceProvider, env, sentry, loggerFactory)
    {
        this.awsOptions = awsOptions.Value;
        this.engiOptions = engiOptions.Value;
    }

    protected override string CreateQuery()
    {
        return @"
            declare function filter(b) {
                return b.ProcessedOn === null && b.SentryId === null
            }

            from QueueEngineRequestCommands as c where filter(c) 
        ";
    }

    protected override async Task ProcessBatchAsync(SubscriptionBatch<QueueEngineRequestCommand> batch, IServiceProvider serviceProvider)
    {
        using var session = batch.OpenAsyncSession();

        var client = new AmazonSimpleNotificationServiceClient(new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = awsOptions.ServiceUrl
        });

        foreach (var item in batch.Items)
        {
            var command = item.Result;

            try
            {
                string json = JsonSerializer.Serialize(new
                {
                    command.Identifier,
                    Command = command.CommandString,
                    sns_sqs_return_channel = engiOptions.EngineOutputTopicName
                }, SerializerOptions);

                await client.PublishAsync(engiOptions.EngineInputTopicArn, json);

                command.ProcessedOn = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                command.SentryId = Sentry.CaptureException(ex, new()
                {
                    ["command"] = command.Id
                }).ToString();

                Logger.LogWarning(ex,
                    "Processing command {command} failed; sentry id={sentryId}.",
                    command.Id, command.SentryId);
            }

            await session.SaveChangesAsync();
        }
    }
}
