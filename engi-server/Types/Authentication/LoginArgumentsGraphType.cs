using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Authentication;

public class LoginArgumentsGraphType : InputObjectGraphType<LoginArguments>
{
    public LoginArgumentsGraphType()
    {
        Description = "Required parameters to login.";

        Field(x => x.Address, type: typeof(AddressGraphType))
            .Description("The address (public key) that belongs to the user trying to login e.g. 5GrwvaEF5zXb26Fz9rcQpDWS57CtERHpNehXCPcNoHGKutQY");
        Field(x => x.Signature, type: typeof(SignatureArgumentsGraphType))
            .Description("The signature generated with the user's private key.");
    }
}
