using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Authentication;

public class LoginArguments : SignedMutationArguments
{
    [Required]
    public string Address { get; set; } = null!;
}