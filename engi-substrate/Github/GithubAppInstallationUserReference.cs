using Octokit;

namespace Engi.Substrate.Github;

public class GithubAppInstallationUserReference
{
    public string Id { get; set; } = null!;

    public long InstallationId { get; set; }

    public string UserId { get; set; } = null!;

    private GithubAppInstallationUserReference() { }

    public GithubAppInstallationUserReference(Installation installation, Identity.User user)
    {
        Id = KeyFrom(installation.Id);
        InstallationId = installation.Id;
        UserId = user.Id;
    }

    public static string KeyFrom(long installationId)
    {
        return $"GithubAppInstallationUserReferences/${installationId}";
    }
}