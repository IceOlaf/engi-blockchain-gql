using Engi.Substrate.Identity;
using Engi.Substrate.Pallets;
using System.Numerics;

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

    public BigInteger? Balance { get; set; }

    public Address? Wallet { get; set; }

    public UserType? UserType { get; set; }

    public static implicit operator CurrentUserInfo(User user)
    {
        return new CurrentUserInfo(user, null, null);
    }

    public CurrentUserInfo(User user, Address? address, AccountInfo? info)
    {
        Email = user.Email;
        Display = user.Display;
        ProfileImageUrl = user.ProfileImageUrl;
        FreelancerSettings = user.FreelancerSettings;
        BusinessSettings = user.BusinessSettings;
        EmailSettings = user.EmailSettings;
        CreatedOn = user.CreatedOn;
        GithubEnrollments = user.GithubEnrollments.Values.ToArray();
        Balance = info == null ? 0 : info.Data.Free;
        Wallet = address;
        UserType = user.UserType;
    }
}
