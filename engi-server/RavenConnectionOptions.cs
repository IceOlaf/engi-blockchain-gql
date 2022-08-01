namespace  Engi.Substrate.Server;

public class RavenConnectionOptions
{
    public string[] Urls { get; set; } = null!;

    public string Database { get; set; } = null!;

    public string? Certificate { get; set; }
}