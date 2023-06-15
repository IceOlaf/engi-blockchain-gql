using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Async;

public class SolveJobCommand
{
    public string Id { get; set; } = null!;
    
    public string JobAttemptedSnapshotId { get; set; } = null!;

    public EngineAttemptResult EngineResult { get; set; } = null!;

    public ulong? SolutionId { get; set; }

    public string? ResultHash { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public string? SentryId { get; set; }

    public static string KeyFrom(string attemptId) {
        return $"SolveJobCommand/{attemptId}";
    }
}
