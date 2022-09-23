using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types;

public class ConfirmEmailArguments
{
    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;
}