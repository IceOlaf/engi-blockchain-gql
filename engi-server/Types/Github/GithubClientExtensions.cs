using Octokit;

namespace Engi.Substrate.Server.Types.Github;

internal static class GithubClientExtensions
{
    public static async Task<GitHubClient> CloneForAsync(this GitHubClient octokit, Installation installation)
    {
        var token = await octokit.GitHubApps.CreateInstallationToken(installation.Id);

        return new GitHubClient(octokit.Connection)
        {
            Credentials = new Credentials(token.Token)
        };
    }
}