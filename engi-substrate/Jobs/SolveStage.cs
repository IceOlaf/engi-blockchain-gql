namespace Engi.Substrate.Jobs;

public class SolveStage
{
    public StageStatus Status { get; set; } = StageStatus.InProgress;

    public SolutionResult? Results { get; set; } = null;
}
