using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate;

public abstract class SignedExtrinsicArgumentsBase : ISignedExtrinsic
{
    [Required]
    public string SenderKeypairPkcs8 { get; set; } = null!;

    public byte Tip { get; set; }
}