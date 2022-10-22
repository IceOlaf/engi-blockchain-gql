using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Authentication;

public class LoginResultGraphType : ObjectGraphType<LoginResult>
{
    public LoginResultGraphType()
    {
        Field(x => x.AccessToken)
            .Description("A short-lived JWT access token.");

        Field(x => x.User, type: typeof(CurrentUserInfoGraphType))
            .Description("The logged-in user information.");
    }
}