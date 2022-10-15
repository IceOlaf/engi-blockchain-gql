using Engi.Substrate.Github;
using Engi.Substrate.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Controllers;

[Route("api/webhooks"), ApiController, AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IAsyncDocumentSession session;
    private readonly EngiOptions options;

    public WebhooksController(
        IAsyncDocumentSession session,
        IOptions<EngiOptions> options)
    {
        this.session = session;
        this.options = options.Value;
    }

    [HttpPost("github")]
    public async Task<IActionResult> Github()
    {
        string webhookId = Request.Headers["X-GitHub-Hook-ID"];

        JObject json;

        using (var reader = new StreamReader(Request.Body))
        {
            json = await JObject.LoadAsync(new JsonTextReader(reader));
        }

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
            else if (action is "removed" or "added")
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

                    if (user.GithubEnrollments?.TryGetValue(payload.Installation.Id, out var enrollment) == true)
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
}