namespace Engi.Substrate.Server.Async;

public class QueueEngineRequestCommand
{
    public string Id { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public string CommandString { get; set; } = null!;

    public DateTime? ProcessedOn { get; set; }

    public string? SentryId { get; set; }
}