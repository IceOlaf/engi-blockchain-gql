using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Github;

public class GithubEnrollmentArguments
{
    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public string InstallationId { get; set; } = null!;
}