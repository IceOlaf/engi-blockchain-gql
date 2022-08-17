using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class CreateJobArguments : JobDefinition, ISignedExtrinsic
{
    public string SenderSecret { get; set; } = null!;

    public byte Tip { get; set; }
}