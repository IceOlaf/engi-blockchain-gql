using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class CreateJobInput : SignedExtrinsicInputBase
{
    public JobDefinition Job { get; set; } = null!;
}