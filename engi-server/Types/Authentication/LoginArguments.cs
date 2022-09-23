using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Authentication;

public class LoginArguments
{
    [Required]
    public string Address { get; set; } = null!;

    [Required]
    public SignatureArguments Signature { get; set; } = null!;
}