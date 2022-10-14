using Octokit;

namespace Engi.Substrate.Github;

public class GithubAppInstallationPayload
{
    public string Action { get; set; } = null!;

    public User Sender { get; set; } = null!;

    public Installation Installation { get; set; } = null!;

    public GithubRepository[] Repositories { get; set; } = null!;
}