using Engi.Substrate.Jobs;

namespace Engi.Substrate.Identity;

public class UserBusinessSettings
{
    public string CompanyName { get; set; } = null!;

    public Language[] PreferredLanguages { get; set; } = Array.Empty<Language>();
}
