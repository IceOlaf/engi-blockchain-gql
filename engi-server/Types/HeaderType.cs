using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class HeaderType : ObjectGraphType<Header>
{
    public HeaderType()
    {
        Field(x => x.Number)
            .Description("The header number.");
        Field(x => x.ParentHash)
            .Description("The block's parent hash.");
        Field(x => x.ExtrinsicsRoot);
        Field(x => x.StateRoot);
        Field(x => x.Digest);
        Field("Hash", x => x.Hash.Value)
            .Description("The calculated hash.");
    }
}