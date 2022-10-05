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

        var (organization, name) = RepositoryUrl.Parse(repository.Url);

        repository.Organization = organization;
        repository.Name = name;
        repository.FullName = $"{organization}/{name}";

        return repository;
    }
}