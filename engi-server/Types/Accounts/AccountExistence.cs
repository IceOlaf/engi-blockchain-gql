namespace Engi.Substrate.Server.Types;

public class AccountExistence
{
    public Address Address { get; set; } = null!;

    public bool Exists { get; set; }
}
