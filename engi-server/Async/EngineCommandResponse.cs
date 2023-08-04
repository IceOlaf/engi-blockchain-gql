using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Async;

public class EngineCommandResponse
{
    public string Id { get; set; } = null!;

    public CommandLineExecutionResult ExecutionResult { get; set; } = null!;

    public static string KeyFrom(string id) {
        return $"EngineCommandResponse/{id}";
    }
}
