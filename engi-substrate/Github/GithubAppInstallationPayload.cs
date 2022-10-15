using Octokit;

namespace Engi.Substrate.Github;

public abstract class GithubAppInstallationPayload
{
    public string Action { get; set; } = null!;

    public User Sender { get; set; } = null!;

    public Installation Installation { get; set; } = null!;
}

public sealed class GithubAppInstallationCreatedPayload : GithubAppInstallationPayload
{
    public GithubRepository[] Repositories { get; set; } = null!;
}

public sealed class GithubAppInstallationDeletedPayload : GithubAppInstallationPayload
{
    public GithubRepository[] Repositories { get; set; } = null!;
}

public sealed class GithubAppInstallationRepositoriesChangedPayload : GithubAppInstallationPayload
{
    public GithubRepository[] RepositoriesAdded { get; set; } = null!;

    public GithubRepository[] RepositoriesRemoved { get; set; } = null!;
}
