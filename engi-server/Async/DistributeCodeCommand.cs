namespace Engi.Substrate.Server.Async;

public class DistributeCodeCommand
{
    public ulong JobId { get; set; }

    public ulong SolutionId { get; set; }

    public string? PullRequestUrl { get; set; }

    public DateTime? FirstDeferredOn { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public string? SentryId { get; set; }
}