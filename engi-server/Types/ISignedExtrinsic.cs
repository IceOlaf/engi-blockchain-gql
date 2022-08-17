namespace Engi.Substrate.Server.Types;

public interface ISignedExtrinsic
{
    string SenderSecret { get; set; }

    byte Tip { get; set; }
}