namespace Engi.Substrate.Server.Types;

public class BalanceTransferInput
{
    public string SenderSecret { get; set; } = null!;

    public string RecipientAddress { get; set; } = null!;

    public ulong Amount { get; set; }

    public byte Tip { get; set; }
}