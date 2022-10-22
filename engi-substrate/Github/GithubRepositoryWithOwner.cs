namespace Engi.Substrate.Github;

public class GithubRepositoryWithOwner : GithubRepository
{
    public GithubRepositoryOwner Owner { get; set; } = null!;
}