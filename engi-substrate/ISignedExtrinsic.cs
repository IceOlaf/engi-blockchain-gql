namespace Engi.Substrate;

public interface ISignedExtrinsic
{
    string SenderSecret { get; set; }

    byte Tip { get; set; }
}