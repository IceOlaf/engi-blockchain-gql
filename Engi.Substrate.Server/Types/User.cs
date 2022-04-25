namespace Engi.Substrate.Server.Types;

public class User
{
    public string Name { get; set; } = null!;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public string Encoded { get; set; } = null!;

    public string Address { get; set; } = null!;

    public AccountMetadata Metadata { get; set; } = null!;
}