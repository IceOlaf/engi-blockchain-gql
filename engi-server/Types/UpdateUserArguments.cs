using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types;

public class UpdateUserArguments
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? Display { get; set; }
}
