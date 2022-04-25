using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AccountMetadataType : ObjectGraphType<AccountMetadata>
{
    public AccountMetadataType()
    {
        Field(x => x.Content);
        Field(x => x.Type);
        Field(x => x.Version);
    }
}