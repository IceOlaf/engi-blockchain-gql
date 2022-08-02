namespace Engi.Substrate.Server.Types;

public class BalanceTransferArguments : SignedExtrinsicArgumentsBase
{
    public string RecipientAddress { get; set; } = null!;

    public ulong Amount { get; set; }
}