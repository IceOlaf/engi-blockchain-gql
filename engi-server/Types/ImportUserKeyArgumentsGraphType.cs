using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class ImportUserKeyArgumentsGraphType : InputObjectGraphType<ImportUserKeyArguments>
{
    public ImportUserKeyArgumentsGraphType()
    {
        Field(x => x.EncryptedPkcs8Key, nullable: true)
            .Description("The user's key, packaged in a PKCS8 envelope and encrypted (with PKCS1 padding) with ENGI's public key.");
    }
}