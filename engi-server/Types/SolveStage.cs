using Engi.Substrate.Jobs;
namespace Engi.Substrate.Server.Types;

public class SolveStage
{
    public StageStatus Status { get; set; } = StageStatus.InProgress;

    public SolutionResult? Results { get; set; } = null;
}
