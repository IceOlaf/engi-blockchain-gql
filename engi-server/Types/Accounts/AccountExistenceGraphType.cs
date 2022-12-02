using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AccountExistenceGraphType : ObjectGraphType<AccountExistence>
{
    public AccountExistenceGraphType()
    {
        Description = "Denotes the existence of an ENGI account for a particular address.";

        Field(x => x.Address)
            .Description("The address.");

        Field(x => x.Exists)
            .Description("A boolean denoting whether an account with that address exists.");
    }
}