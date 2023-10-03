namespace Engi.Substrate.Server.Types.Authentication;

public class LoginResult
{
    public string? SessionToken { get; set; } = null;
    public CurrentUserInfo User { get; set; } = null!;
}
