using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Authentication;

public sealed class SignatureArgumentsGraphType : InputObjectGraphType<SignatureArguments>
{
    public SignatureArgumentsGraphType()
    {
        Description = "Required parameters to invoke a signed mutation.";

        Field(x => x.Value)
            .Description("The hex-formatted signature, calculated with the user's private key, for the string `{address}|{unixTimeMs}`, where 'address' is the address submitted and 'unixTimeMs' the current time, in milliseconds since the UNIX epoch.");
        Field(x => x.SignedOn)
            .Description("The date and time (ISO 8601) the signature was produced, which must match the timestamp in the signature string.");
    }
}