namespace Engi.Substrate.Server.Types.Authentication;

public class LogoutResult
{
    public string Result { get; set; } = null!;

    public LogoutResult()
    {
        Result = "goodbye";
    }
}
