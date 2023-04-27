using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using RazorLight;
using SendGrid;
using SendGrid.Helpers.Mail;
using Sentry;
using Constants = Raven.Client.Constants;

namespace Engi.Substrate.Server.Email;

public class EmailDispatchCommandProcessor : SubscriptionProcessingBase<EmailDispatchCommand>
{
    public EmailDispatchCommandProcessor(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        IHub sentry,
        IWebHostEnvironment env,
        ILoggerFactory loggerFactory)
        : base(store, serviceProvider, env, sentry, loggerFactory)
    { }

    protected override string CreateQuery()
    {
        return @"
            declare function filter(c) {
                return c.SentOn === null && c.SentryId === null;
            }

            from EmailDispatchCommands as c where filter(c) include c.UserId
        ";
    }

    protected override async Task ProcessBatchAsync(SubscriptionBatch<EmailDispatchCommand> batch, IServiceProvider serviceProvider)
    {
        Logger.LogInformation("Processing email dispatch");
        var razorlight = serviceProvider.GetRequiredService<IRazorLightEngine>();
        var sendgrid = serviceProvider.GetRequiredService<SendGridClient>();
        var emailOptions = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;
        var applicationOptions = serviceProvider.GetRequiredService<IOptions<ApplicationOptions>>().Value;

        var from = new EmailAddress(emailOptions.SenderEmail, emailOptions.SenderName);

        using var session = batch.OpenAsyncSession();

        Logger.LogInformation("{} email dispatch to process");
        foreach (var item in batch.Items)
        {
            var command = item.Result;

            var user = await session.LoadAsync<Identity.User>(command.UserId);

            string[] result;

            try
            {
                result = await razorlight.RenderSubjectAndContentAsync(command.TemplateName, new EmailModel
                {
                    Application = applicationOptions,
                    User = user,
                    Data = command.Data ?? new()
                });
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

                continue;
            }

            var to = new EmailAddress(user.Email, user.Display);

            var message = MailHelper.CreateSingleEmail(from, to, result[0].Trim(), result[1], result[2]);

            try
            {
                var response = await sendgrid.SendEmailAsync(message);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        await response.Body.ReadAsStringAsync());
                }

                command.SentOn = DateTime.UtcNow;
            }
            catch (Exception ex) when (ex is IOException or EndOfStreamException or TimeoutException)
            {
                // network exception, retry later

                var metadata = session.Advanced.GetMetadataFor(command);

                metadata[Constants.Documents.Metadata.Refresh] = DateTime.UtcNow.AddMinutes(1);
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
        }

        await session.SaveChangesAsync();
        Logger.LogInformation("{Done processing email dispatch");
    }
}
