using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AccountMetadataGraphType : ObjectGraphType<AccountMetadata>
{
    public AccountMetadataGraphType()
    {
        Description = "Metadata associated with an account key pair created using the API.";

        Field(x => x.Content)
            .Description("The content type; currently equal to 'pkcs8', 'sr25519'.");
        Field(x => x.Type)
            .Description("The content encryption; either 'none' or 'scrypt', 'xsalsa20-poly1305'.");
        Field(x => x.Version)
            .Description("The version of the wallet created; currently v3.");
    }
}