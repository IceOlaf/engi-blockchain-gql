namespace Engi.Substrate;

public class ExtrinsicSignature
{
    public MultiAddress Address { get; init; } = null!;

    public byte[] Signature { get; init; } = null!;

    public ExtrinsicEra Era { get; set; } = null!;

    public byte Nonce { get; set; }

    public byte Tip { get; set; }
}