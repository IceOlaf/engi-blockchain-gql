namespace Engi.Substrate.Identity;

public abstract class UserToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Type { get; set; }

    public string? Value { get; protected set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresOn { get; set; }

    protected UserToken()
    {
        Type = GetType().Name;
    }
}