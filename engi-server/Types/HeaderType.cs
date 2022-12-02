using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class HeaderGraphType : ObjectGraphType<Header>
{
    public HeaderGraphType()
    {
        Description = "The block header.";

        Field(x => x.Number)
            .Description("The header number.");
        Field(x => x.ParentHash)
            .Description("The block's parent hash.");
        Field(x => x.ExtrinsicsRoot);
        Field(x => x.StateRoot);
        Field(x => x.Digest, type: typeof(DigestGraphType));
        Field("Hash", x => x.Hash.Value)
            .Description("The calculated hash.");
    }
}