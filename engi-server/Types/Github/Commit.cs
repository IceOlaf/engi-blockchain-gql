namespace Engi.Substrate.Server.Types.Github;

public class Commit
{
    public string Sha { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Author { get; set; } = null!;

    public string Committer { get; set; } = null!;
}