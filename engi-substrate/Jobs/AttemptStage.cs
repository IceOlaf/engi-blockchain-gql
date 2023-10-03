namespace Engi.Substrate.Jobs;

public class AttemptStage
{
    public StageStatus Status { get; set; } = StageStatus.InProgress;

    public CommandLineExecutionResult? Results { get; set; } = null;

    public TestAttempt[]? Tests { get; set; } = null;
}
