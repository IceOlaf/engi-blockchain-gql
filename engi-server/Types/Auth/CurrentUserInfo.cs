using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Types;

public class CurrentUserInfo
{
    public string Email { get; set; } = null!;

    public string Display { get; set; } = null!;

    public UserFreelancerSettings? FreelancerSettings { get; set; }

    public UserEmailSettings EmailSettings { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public UserGithubEnrollment[] GithubEnrollments { get; set; } = null!;

    public static implicit operator CurrentUserInfo(User user)
    {
        return new CurrentUserInfo
        {
            Email = user.Email,
            Display = user.Display,
            FreelancerSettings = user.FreelancerSettings,
            EmailSettings = user.EmailSettings,
            CreatedOn = user.CreatedOn,
            GithubEnrollments = user.GithubEnrollments.Values.ToArray()
        };
    }
}
