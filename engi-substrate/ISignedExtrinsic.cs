namespace Engi.Substrate;

public interface ISignedExtrinsic
{
    string SenderKeypairPkcs8 { get; set; }

    byte Tip { get; set; }
}