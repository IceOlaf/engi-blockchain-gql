using Newtonsoft.Json.Linq;

namespace Engi.Substrate.Github;

public class GithubAppUnknownWebhookEvent
{
    public GithubAppUnknownWebhookEvent(JObject payload, string webhookId)
    {
        Id = $"GithubUnknownWebhookEvents/{webhookId}";
        WebhookId = webhookId;
        Payload = payload;
    }

    public string Id { get; set; } = null!;

    public string WebhookId { get; set; }

    public JObject Payload { get; set; }
}