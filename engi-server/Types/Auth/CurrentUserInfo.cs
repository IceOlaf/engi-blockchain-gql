using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Types;

public class CurrentUserInfo
{
    public string Email { get; set; } = null!;

    public string Display { get; set; } = null!;

    public string? ProfileImageUrl { get; set; }

    public UserFreelancerSettings? FreelancerSettings { get; set; }

    public UserBusinessSettings? BusinessSettings { get; set; }

    public UserEmailSettings EmailSettings { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public UserGithubEnrollment[] GithubEnrollments { get; set; } = null!;

    public static implicit operator CurrentUserInfo(User user)
    {
        return new CurrentUserInfo
        {
            Email = user.Email,
            Display = user.Display,
            ProfileImageUrl = user.ProfileImageUrl,
            FreelancerSettings = user.FreelancerSettings,
            BusinessSettings = user.BusinessSettings,
            EmailSettings = user.EmailSettings,
            CreatedOn = user.CreatedOn,
            GithubEnrollments = user.GithubEnrollments.Values.ToArray()
        };
    }
}
