using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class BlockReferenceGraphType : ObjectGraphType<BlockReference>
{
    public BlockReferenceGraphType()
    {
        Description = "Information about a block related to a parent object.";

        Field(x => x.Number)
            .Description("The block number.");
        Field(x => x.DateTime)
            .Description("The date/time that block was produced.");
    }
}