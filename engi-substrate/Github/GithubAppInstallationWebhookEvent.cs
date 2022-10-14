namespace Engi.Substrate.Github;

public class GithubAppInstallationWebhookEvent
{
    private GithubAppInstallationWebhookEvent() { }

    public GithubAppInstallationWebhookEvent(GithubAppInstallationPayload payload, string webhookId)
    {
        Id = $"GithubAppInstallationWebhookEvents/{webhookId}";
        WebhookId = webhookId;
        Payload = payload;
    }

    public string Id { get; set; } = null!;

    public string WebhookId { get; set; }

    public GithubAppInstallationPayload Payload { get; set; }
}