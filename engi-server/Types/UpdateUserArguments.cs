using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class UpdateUserArguments
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? Display { get; set; }

    public UserFreelancerSettings? FreelancerSettings { get; set; }

    public UserEmailSettings? EmailSettings { get; set; }
}
