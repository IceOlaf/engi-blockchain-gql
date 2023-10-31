using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Types;

public class UpdateUserArguments
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? Display { get; set; }

    public string? ProfileImageUrl { get; set; }

    public UserType? UserType { get; set; }

    public UserFreelancerSettings? FreelancerSettings { get; set; }

    public UserBusinessSettings? BusinessSettings { get; set; }

    public UserEmailSettings? EmailSettings { get; set; }
}
