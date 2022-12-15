using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types;

public class CreateUserArguments
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, StringLength(50, MinimumLength = 4)]
    public string Display { get; set; } = null!;

    [Required]
    public Address Address { get; set; } = null!;
}
