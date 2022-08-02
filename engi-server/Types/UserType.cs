using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserType : ObjectGraphType<User>
{
    public UserType()
    {
        Field(x => x.Name)
            .Description("The name of the account.");
        Field(x => x.Address)
            .Description("The address that corresponds to the private key.");
        Field(x => x.CreatedOn)
            .Description("The date the account was created.");
        Field(x => x.Encoded)
            .Description("The encoded/encrypted wallet in PKCS8 format.");
        Field(x => x.Metadata)
            .Description("The wallet metadata.");
    }
}