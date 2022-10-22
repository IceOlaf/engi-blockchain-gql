namespace Engi.Substrate.Server.Types.Authentication;

public class LoginResult : AuthenticationTokenPair
{
    public CurrentUserInfo User { get; set; } = null!;
}