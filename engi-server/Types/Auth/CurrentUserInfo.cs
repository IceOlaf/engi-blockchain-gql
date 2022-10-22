using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Types;

public class CurrentUserInfo
{
    public string Display { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public UserGithubEnrollment[] GithubEnrollments { get; set; } = null!;
}