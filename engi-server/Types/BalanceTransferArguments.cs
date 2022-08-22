using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types;

public class BalanceTransferArguments : SignedExtrinsicArgumentsBase
{
    [Required]
    public string RecipientAddress { get; set; } = null!;

    [Range(1, ulong.MaxValue)]
    public ulong Amount { get; set; }
}