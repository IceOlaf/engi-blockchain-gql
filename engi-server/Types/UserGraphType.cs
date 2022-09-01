using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        Description = "An ENGI user.";

        Field(x => x.Name)
            .Description("The name of the account.");
        Field(x => x.Address)
            .Description("The address that corresponds to the private key.");
        Field(x => x.CreatedOn)
            .Description("The date the account was created.");
        Field(x => x.Encoded)
            .Description("The encoded/encrypted wallet in PKCS8 format. This can be used to invoke mutations that correspond to signed extrinsics.");
        Field(x => x.Metadata)
            .Description("The wallet metadata.");
    }
}