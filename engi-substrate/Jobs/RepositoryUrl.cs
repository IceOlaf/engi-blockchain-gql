namespace Engi.Substrate.Jobs;

public static class RepositoryUrl
{
    public static (string organization, string name) Parse(string url)
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