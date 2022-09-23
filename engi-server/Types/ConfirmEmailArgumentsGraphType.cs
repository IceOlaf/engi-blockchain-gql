using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class ConfirmEmailArgumentsGraphType : InputObjectGraphType<ConfirmEmailArguments>
{
    public ConfirmEmailArgumentsGraphType()
    {
        Description = "Arguments for confirming a user's email. The link sent out is in the form `https://{domain}/confirm/{token}?id={userId}`.";

        Field(x => x.UserId)
            .Description("The user's id");
        Field(x => x.Token)
            .Description("The confirmation token");
    }
}