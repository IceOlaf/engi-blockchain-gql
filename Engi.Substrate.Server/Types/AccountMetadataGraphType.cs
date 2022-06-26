using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AccountMetadataGraphType : ObjectGraphType<AccountMetadata>
{
    public AccountMetadataGraphType()
    {
        Field(x => x.Content);
        Field(x => x.Type);
        Field(x => x.Version);
    }
}