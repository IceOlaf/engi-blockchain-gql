using Engi.Substrate.Jobs;
namespace Engi.Substrate.Server.Types;

public class SolutionResult
{
    public ulong? SolutionId { get; set; } = null!;

    public string? ResultHash { get; set; } = null!;
}
