using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Authentication;

public class AuthenticationTokenPairGraphType : ObjectGraphType<AuthenticationTokenPair>
{
    public AuthenticationTokenPairGraphType()
    {
        Field(x => x.AccessToken)
            .Description("A short-lived JWT access token.");
    }
}