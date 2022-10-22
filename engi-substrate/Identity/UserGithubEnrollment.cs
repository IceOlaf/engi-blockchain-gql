using Engi.Substrate.Github;

namespace Engi.Substrate.Identity;

public class UserGithubEnrollment
{
    public long InstallationId { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public GithubRepositoryOwner Owner { get; set; } = null!;

    public List<GithubRepository> Repositories { get; set; } = null!;

    public void Add(GithubRepository[] repositories)
    {
        string[] fullNames = repositories
            .Select(x => x.FullName)
            .ToArray();

        foreach (var repo in repositories)
        {
            if (!fullNames.Contains(repo.FullName))
            {
                Repositories.Add(repo);
            }
        }
    }

    public void Remove(GithubRepository[] repositories)
    {
        string[] fullNames = repositories
            .Select(x => x.FullName)
            .ToArray();

        Repositories.RemoveAll(x => fullNames.Contains(x.FullName));
    }
}