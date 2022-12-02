using GraphQL;

namespace Engi.Substrate.Server.Types.Authentication;

public class AuthenticationError : ExecutionError
{
    public AuthenticationError()
        : base("Authentication failed.")
    {
        Code = "AUTHENTICATION_FAILED";
    }
}