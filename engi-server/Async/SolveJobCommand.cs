using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Async;

public class SolveJobCommand
{
    public string Id { get; set; } = null!;
    
    public string JobAttemptedSnapshotId { get; set; } = null!;

    public EngineExecutionResult EngineResult { get; set; } = null!;

    public ulong? SolutionId { get; set; }

    public string? ResultHash { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public string? SentryId { get; set; }
}
