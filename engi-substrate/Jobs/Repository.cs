namespace Engi.Substrate.Jobs;

public class Repository
{
    public string Url { get; set; } = null!;

    public string Branch { get; set; } = null!;

    public string Commit { get; set; } = null!;

    public string? Organization { get; set; }

    public string? Name { get; set; }

    public string? FullName { get; set; }

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

    internal static (string? organization, string? name) ParseFullName(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return (null, null);
        }

        if (!uri.Host.EndsWith("github.com") && !uri.Host.EndsWith("gitlab.com"))
        {
            return (null, null);
        }

        string pathName = uri.AbsolutePath.TrimStart('/');

        if (pathName.EndsWith(".git"))
        {
            pathName = pathName.Substring(0, pathName.Length - 4);
        }

        string[] parts = pathName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            return (null, null);
        }

        return (parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant());
    }
}