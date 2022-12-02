namespace Engi.Substrate.Server.Types;

public class AccountExistence
{
    public string Address { get; set; } = null!;

    public bool Exists { get; set; }
}