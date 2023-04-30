using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.SecurityToken.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Engi.Substrate.Jobs;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Sentry;

namespace Engi.Substrate.Server.Async;

public class QueueEngineRequestCommandService : SubscriptionProcessingBase<QueueEngineRequestCommand>
{
    private readonly Func<Task<Credentials>> credentialsFactory;
    private readonly AwsOptions awsOptions;
    private readonly EngiOptions engiOptions;

    public QueueEngineRequestCommandService(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        Func<Task<Credentials>> credentialsFactory,
        IHub sentry,
        IOptions<AwsOptions> awsOptions,
        IOptions<EngiOptions> engiOptions,
        ILoggerFactory loggerFactory)
        : base(store, serviceProvider, sentry, engiOptions, loggerFactory)
    {
        this.credentialsFactory = credentialsFactory;
        this.awsOptions = awsOptions.Value;
        this.engiOptions = engiOptions.Value;
    }

    protected override string CreateQuery()
    {
        return @"
            declare function filter(b) {
                return b.ProcessedOn === null && b.SentryId === null
            }

            from QueueEngineRequestCommands as c where filter(c) include c.SourceId
        ";
    }

    protected override async Task ProcessBatchAsync(SubscriptionBatch<QueueEngineRequestCommand> batch, IServiceProvider serviceProvider)
    {
        var credentials = await credentialsFactory();

        var client = new AmazonSimpleNotificationServiceClient(credentials,
            new AmazonSimpleNotificationServiceConfig().Apply(awsOptions));

        using var session = batch.OpenAsyncSession();

        foreach (var item in batch.Items)
        {
            var command = item.Result;

            IDispatched? dispatched = null;

            if(command.SourceId != null)
            {
                var source = await session.LoadAsync<object>(command.SourceId);

                dispatched = source as IDispatched;
            }

            try
            {
                string json = JsonSerializer.Serialize(new
                {
                    command.Identifier,
                    Command = command.CommandString,
                    sns_sqs_return_channel = engiOptions.EngineOutputTopicName
                }, SerializerOptions);

                await client.PublishAsync(new PublishRequest
                {
                    TopicArn = engiOptions.EngineInputTopicArn,
                    Message = json,
                    MessageGroupId = command.Identifier,
                    MessageDeduplicationId = CalculateSha256(json)
                });

                if(dispatched != null)
                {
                    dispatched.DispatchedOn = DateTime.UtcNow;
                }

                command.ProcessedOn = DateTime.UtcNow;

                Logger.LogInformation("Successfully published message to engine topic.");
                Logger.LogTrace("Published message={message} topic={topic}", json, engiOptions.EngineInputTopicArn);
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

    private static string CalculateSha256(string s)
    {
        using var hash = SHA256.Create();

        byte[] result = hash.ComputeHash(Encoding.UTF8.GetBytes(s));

        return Hex.GetString(result);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
