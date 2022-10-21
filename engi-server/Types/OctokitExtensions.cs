using Octokit;

namespace Engi.Substrate.Server.Types;

public static class OctokitExtensions
{
    public static async Task<Repository> GetOrCreateRepositoryForCurrentUser(
        this GitHubClient client,
        string name)
    {
        var currentUser = await client.User.Current();

        string owner = currentUser.Login;

        try
        {
            return await client.Repository.Get(owner, name);
        }
        catch (Octokit.NotFoundException)
        {
            return await client.Repository.Create(new NewRepository(name)
            {
                HasDownloads = false,
                HasIssues = false,
                HasProjects = false,
                HasWiki = false
            });
        }
    }
}