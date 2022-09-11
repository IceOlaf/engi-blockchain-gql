using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Authentication;

public class LoginArgumentsGraphType : InputObjectGraphType<LoginArguments>
{
    public LoginArgumentsGraphType()
    {
        Description = "Required parameters to login.";

        Field(x => x.Address)
            .Description("The address (public key) that belongs to the user trying to login e.g. 5GrwvaEF5zXb26Fz9rcQpDWS57CtERHpNehXCPcNoHGKutQY");
        Field(x => x.Signature)
            .Description("The hex-formatted signature, calculated with the user's private key, for the string `{address}|{unixTimeMs}`, where 'address' is the address submitted and 'unixTimeMs' the current time, in milliseconds since the UNIX epoch.");
        Field(x => x.SignedOn)
            .Description("The date and time (ISO 8601) the signature was produced, which must match the timestamp in the signature string.");
    }
}