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

        if (json.ContainsKey("installation"))
        {
            var serializer = new Octokit.Internal.SimpleJsonSerializer();

            var payload = serializer.Deserialize<GithubAppInstallationPayload>(json.ToString());

            if (payload.Action == "deleted")
            {
                // find user and remove reference

                var referenceId = GithubAppInstallationUserReference.KeyFrom(payload.Installation.Id);

                var reference = await session
                    .LoadAsync<GithubAppInstallationUserReference>(referenceId,
                        include => include.IncludeDocuments(x => x.UserId));

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
            }

            var @event = new GithubAppInstallationWebhookEvent(payload, webhookId);

            await session.StoreAsync(@event,
                null, // overwrite if re-delivered
                @event.Id);
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