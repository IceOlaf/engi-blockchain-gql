namespace Engi.Substrate.Server.Types;

public class AccountExistence
{
    public string Address { get; set; } = null!;

    public AccountExistenceResult Exists { get; set; }
}
