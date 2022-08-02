using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class CreateJobArguments : SignedExtrinsicArgumentsBase
{
    public JobDefinition Job { get; set; } = null!;
}