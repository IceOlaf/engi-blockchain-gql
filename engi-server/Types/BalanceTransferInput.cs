namespace Engi.Substrate.Server.Types;

public class BalanceTransferInput : SignedExtrinsicInputBase
{
    public string RecipientAddress { get; set; } = null!;

    public ulong Amount { get; set; }
}