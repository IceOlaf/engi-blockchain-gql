using System.Security.Cryptography;
using System.Text;
using Engi.Substrate.Github;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Sentry;
using User = Engi.Substrate.Identity.User;

namespace Engi.Substrate.Server.Controllers;

[Route("api/webhooks"), ApiController, AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IAsyncDocumentSession session;
    private readonly IHub sentry;
    private readonly EngiOptions options;

    public WebhooksController(
        IAsyncDocumentSession session,
        IHub sentry,
        IOptions<EngiOptions> options)
    {
        this.session = session;
        this.sentry = sentry;
        this.options = options.Value;
    }

    [HttpPost("github")]
    public async Task<IActionResult> Github()
    {
        if (!Request.Headers.TryGetValue("x-hub-signature-256", out var expectedSignatureStringValues))
        {
            sentry.CaptureMessage("Github webhook received without a signature; is a webhook secret set?");

            return BadRequest();
        }

        string jsonRaw;

        using (var reader = new StreamReader(Request.Body))
        {
            jsonRaw = await reader.ReadToEndAsync();
        }

        var json = JObject.Parse(jsonRaw);

        string expectedSignature = expectedSignatureStringValues;
        string calculatedSignature = CalculateGithubSignature(jsonRaw, options.GithubAppWebhookSecret);

        if (calculatedSignature != expectedSignature)
        {
            return BadRequest();
        }

        string webhookId = Request.Headers["X-GitHub-Hook-ID"];

        var octokitSerializer = new Octokit.Internal.SimpleJsonSerializer();

        if (json.ContainsKey("installation"))
        {
            string action = (string) json["action"]!;

            if (action == "created")
            {
                var payload = octokitSerializer.Deserialize<GithubAppInstallationCreatedPayload>(json.ToString());

                var @event = new GithubAppInstallationWebhookEvent(payload, webhookId);

                await session.StoreAsync(@event, null, @event.Id);
            }
            else if (action == "deleted")
            {
                var payload = octokitSerializer.Deserialize<GithubAppInstallationDeletedPayload>(json.ToString());

                // find user and remove reference

                var reference = await session
                    .LoadAsync<GithubAppInstallationUserReference>(GithubAppInstallationUserReference.KeyFrom(payload.Installation.Id));

                if (reference != null)
                {
                    session.Advanced.Clear();

                    session.Advanced.Defer(new PatchCommandData(reference.UserId, null, new PatchRequest
                    {
                        Script = @"delete this.GithubEnrollments[args.installationId]",
                        Values = new()
                        {
                            ["installationId"] = payload.Installation.Id
                        }
                    }));
                }

                var @event = new GithubAppInstallationWebhookEvent(payload, webhookId);

                await session.StoreAsync(@event, null, @event.Id);
            }
            else if ((action == "removed") || (action == "added"))
            {
                var payload = octokitSerializer.Deserialize<GithubAppInstallationRepositoriesChangedPayload>(json.ToString());

                // find user and update repositories

                var reference = await session
                    .LoadAsync<GithubAppInstallationUserReference>(GithubAppInstallationUserReference.KeyFrom(payload.Installation.Id),
                        include => include.IncludeDocuments(x => x.UserId));

                if (reference != null)
                {
                    // in this case we don't use a patch because:
                    // - we have to load the enrollment to make the changes
                    // - two separate events (removed/added) arrive one after the other and would likely result in a race condition

                    var user = await session.LoadAsync<User>(reference.UserId);

                    if (user.GithubEnrollments.TryGetValue(payload.Installation.Id, out var enrollment))
                    {
                        enrollment.Add(payload.RepositoriesAdded);
                        enrollment.Remove(payload.RepositoriesRemoved);
                    }
                }

                var @event = new GithubAppInstallationWebhookEvent(payload, webhookId);
                await session.StoreAsync(@event, null, @event.Id);
            }
        }
        else
        {
            var @event = new GithubAppUnknownWebhookEvent(json, webhookId);

            await session.StoreAsync(@event,
                null, // overwrite if re-delivered
                @event.Id);
        }

        await session.SaveChangesAsync();

        return Ok();
    }

    private static string CalculateGithubSignature(string payload, string secret)
    {
        var shaKeyBytes = Encoding.UTF8.GetBytes(secret);

        using var sha = new HMACSHA256(shaKeyBytes);

        var signatureHashBytes = sha.ComputeHash(
            Encoding.UTF8.GetBytes(payload));

        return "sha256=" + Hex.GetString(signatureHashBytes);
    }
}
