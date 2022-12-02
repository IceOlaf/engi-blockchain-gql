namespace Engi.Substrate.Identity;

public class User
{
    public string Id { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Display { get; set; } = null!;

    public byte[]? KeypairPkcs8 { get; set; }

    public List<string> SystemRoles { get; } = new();

    public List<UserToken> Tokens { get; } = new();

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? EmailConfirmedOn { get; set; }

    public UserGithubEnrollmentDictionary GithubEnrollments { get; set; } = new();
}
