namespace Engi.Substrate.Server.Types;

public abstract class SignedExtrinsicArgumentsBase : ISignedExtrinsic
{
    public string SenderSecret { get; set; } = null!;

    public byte Tip { get; set; }
}