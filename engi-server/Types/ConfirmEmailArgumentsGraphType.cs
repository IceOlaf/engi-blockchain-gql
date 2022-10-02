using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class ConfirmEmailArgumentsGraphType : InputObjectGraphType<ConfirmEmailArguments>
{
    public ConfirmEmailArgumentsGraphType()
    {
        Description = "Arguments for confirming a user's email. The link sent out is in the form `https://{domain}/signup/confirm/{token}?id={userId}`.";

        Field(x => x.Address)
            .Description("The user's address.");
        Field(x => x.Token)
            .Description("The confirmation token.");
    }
}