namespace Engi.Substrate.Server.Types;

public class AccountMetadata
{
    public string[] Content { get; set; } = null!;

    public string[] Type { get; set; } = null!;

    public int Version { get; set; }
}