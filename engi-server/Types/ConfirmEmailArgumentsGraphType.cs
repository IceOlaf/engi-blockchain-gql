using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class ConfirmEmailArgumentsGraphType : InputObjectGraphType<ConfirmEmailArguments>
{
    public ConfirmEmailArgumentsGraphType()
    {
        Description = "Arguments for confirming a user's email. The link sent out is in the form `https://{domain}/signup/confirm/{address}?id={userId}`.";

        Field(x => x.Address, type: typeof(AddressGraphType))
            .Description("The user's address.");
        Field(x => x.Token)
            .Description("The confirmation token.");
    }
}
