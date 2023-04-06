using Engi.Substrate.Jobs;

namespace Engi.Substrate.Identity;

public class UserBusinessSettings
{
    public string CompanyName { get; set; } = null!;

    public Technology[] PreferredTechnologies { get; set; } = Array.Empty<Technology>();
}
