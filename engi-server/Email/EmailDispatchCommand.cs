namespace Engi.Substrate.Server.Email;

public class EmailDispatchCommand
{
    public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string TemplateName { get; set; } = null!;

    public Dictionary<string, object>? Data { get; set; }

    public DateTime? SentOn { get; set; }

    public string? SentryId { get; set; }
}
