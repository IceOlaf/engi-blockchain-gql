using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Types;

public class CurrentUserInfo
{
    public string Display { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public UserGithubEnrollment[] GithubEnrollments { get; set; } = null!;

    public static implicit operator CurrentUserInfo(User user)
    {
        return new CurrentUserInfo
        {
            Display = user.Display,
            Email = user.Email,
            CreatedOn = user.CreatedOn,
            GithubEnrollments = user.GithubEnrollments.Values.ToArray()
        };
    }
}