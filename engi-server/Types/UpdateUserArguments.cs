using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class UpdateUserArguments
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? Display { get; set; }

    public Language[]? JobPreference { get; set; }
}
