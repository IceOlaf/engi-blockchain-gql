namespace Engi.Substrate.Jobs;

public class Repository
{
    public string Url { get; set; } = null!;

    public string Branch { get; set; } = null!;

    public string Commit { get; set; } = null!;

    public string Organization { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public static Repository Parse(ScaleStreamReader reader)
    {
        var repository = new Repository
        {
            Url = reader.ReadString()!,
            Branch = reader.ReadString()!,
            Commit = reader.ReadString()!
        };

        var (organization, name) = ParseFullName(repository.Url);

        repository.Organization = organization;
        repository.Name = name;
        repository.FullName = $"{organization}/{name}";

        return repository;
    }

    internal static (string organization, string name) ParseFullName(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentOutOfRangeException(
                nameof(url), url, "Invalid URI.");
        }

        if (!uri.Host.EndsWith("github.com") && !uri.Host.EndsWith("gitlab.com"))
        {
            throw new ArgumentOutOfRangeException(
                nameof(url), url, "Only github and gitlab repositories are supported currently.");
        }

        string pathName = uri.AbsolutePath.TrimStart('/');

        if (pathName.EndsWith(".git"))
        {
            pathName = pathName.Substring(0, pathName.Length - 4);
        }

        string[] parts = pathName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(url), url, "Invalid URI.");
        }

        return (parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant());
    }
}