using Engi.Substrate.Identity;
namespace Engi.Substrate.Jobs;

public class UserInfo
{
    public string Address { get; set; } = null!;

    public string? Display { get; set; } = null!;

    public string? ProfileImageUrl { get; set; }

    public UserType? UserType { get; set; }

    public DateTime? CreatedOn { get; set; }

    public int CreatedJobsCount { get; set; }

    public int SolvedJobsCount { get; set; }
}
