namespace Engi.Substrate;

public abstract class SignedExtrinsicArgumentsBase : ISignedExtrinsic
{
    public string SenderSecret { get; set; } = null!;

    public byte Tip { get; set; }
}