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

    private readonly EngiOptions engiOptions;

    public QueueEngineRequestCommandService(
        IDocumentStore store, 
        IServiceProvider serviceProvider,
        IWebHostEnvironment env, 
        IHub sentry, 
        IOptions<EngiOptions> engiOptions,
        ILoggerFactory loggerFactory) 
        : base(store, serviceProvider, env, sentry, loggerFactory)
    {
        this.engiOptions = engiOptions.Value;
    }

    protected override string CreateQuery()
    {
        return @"
            declare function filter(b) {
                return b.ProcessedOn === null && b.SentryId === null
            }

            from QueueEngineProcessCommands as c where filter(c) 
        ";
    }

    protected override async Task ProcessBatchAsync(SubscriptionBatch<QueueEngineRequestCommand> batch, IServiceProvider serviceProvider)
    {
        using var session = batch.OpenAsyncSession();

        var client = new AmazonSimpleNotificationServiceClient();

        foreach (var item in batch.Items)
        {
            var command = item.Result;

            try
            {
                string json = JsonSerializer.Serialize(new
                {
                    command.Identifier,
                    Command = command.CommandString
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
            }

            await session.SaveChangesAsync();
        }
    }
}