using Engi.Substrate.Github;

namespace Engi.Substrate.Identity;

public class UserGithubEnrollment
{
    public long InstallationId { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public GithubRepository[] Repositories { get; set; } = null!;
}