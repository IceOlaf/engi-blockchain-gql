namespace Engi.Substrate.Github;

public class GithubRepository
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public bool IsPrivate { get; set; }

    public bool Equals(string owner, string name)
    {
        string fullName = owner + "/" + name;

        return string.Equals(FullName, fullName, StringComparison.OrdinalIgnoreCase);
    }
}