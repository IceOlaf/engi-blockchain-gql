using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class CurrentUserInfo
{
    public string Email { get; set; } = null!;

    public string Display { get; set; } = null!;

    public Language[] JobPreference { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public UserGithubEnrollment[] GithubEnrollments { get; set; } = null!;

    public static implicit operator CurrentUserInfo(User user)
    {
        return new CurrentUserInfo
        {
            Email = user.Email,
            Display = user.Display,
            JobPreference = user.JobPreference,
            CreatedOn = user.CreatedOn,
            GithubEnrollments = user.GithubEnrollments.Values.ToArray()
        };
    }
}
