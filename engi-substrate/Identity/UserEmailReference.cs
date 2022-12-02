namespace Engi.Substrate.Identity;

public class UserEmailReference
{
    public string Id { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string UserId { get; set; } = null!;

    private UserEmailReference() { }

    public UserEmailReference(User user)
    {
        if (user?.Id == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        Id = KeyFrom(user.Email);
        Email = user.Email;
        UserId = user.Id;
    }

    public static string KeyFrom(string email)
    {
        return $"UserEmailReferences/{email.ToLowerInvariant()}";
    }
}