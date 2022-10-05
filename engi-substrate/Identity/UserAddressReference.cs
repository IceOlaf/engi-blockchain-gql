namespace Engi.Substrate.Identity;

public class UserAddressReference
{
    public string Id { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string UserId { get; set; } = null!;

    private UserAddressReference() { }

    public UserAddressReference(User user)
    {
        if (user?.Id == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (string.IsNullOrEmpty(user.Address))
        {
            throw new ArgumentNullException(nameof(user.Address));
        }

        Id = KeyFrom(user.Address);
        Address = user.Address;
        UserId = user.Id;
    }

    public static string KeyFrom(string address)
    {
        return $"UserAddressReferences/{address}";
    }
}