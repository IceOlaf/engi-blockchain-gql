using System.Numerics;
using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class Engineer
{
    public string DisplayName { get; set; } = null!;
    public string? ProfileImageUrl { get; set; } = null!;
    public string Email { get; set; } = null!;
    public UserType? UserType { get; set; }
    public BigInteger Balance { get; set; }
    public int BountiesSolved { get; set; }
    public int BountiesCreated { get; set; }
    public EngineerEarnings Earnings { get; set; } = null!;
    public Technology[] Techologies { get; set; } = null!;
    public string[] RepositoriesWorkedOn { get; set; } = null!;
    public string RootOrganization { get; set; } = null!;
}
