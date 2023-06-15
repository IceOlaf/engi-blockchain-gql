using Engi.Substrate.Jobs;
namespace Engi.Substrate.Server.Types;

public class AttemptStage
{
    public StageStatus Status { get; set; } = StageStatus.InProgress;

    public CommandLineExecutionResult? Results { get; set; } = null;
}
