namespace Engi.Substrate.Github;

public class GithubRepositoryReadme
{
    public string Id { get; set; } = null!;

    public DateTime RetrievedOn { get; set; }

    public GithubRepositoryOwner? Owner { get; set; }

    public string Content { get; set; } = null!;

    public static string KeyFrom(string repositoryFullName)
    {
        return $"readme/github/{repositoryFullName}";
    }
}
