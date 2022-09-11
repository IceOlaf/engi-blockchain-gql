namespace Engi.Substrate.Identity;

public class UserToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? Value { get; protected set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresOn { get; set; }
}