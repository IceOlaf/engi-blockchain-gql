using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate;

public abstract class SignedExtrinsicArgumentsBase : ISignedExtrinsic
{
    [Required]
    public string SenderSecret { get; set; } = null!;

    public byte Tip { get; set; }
}