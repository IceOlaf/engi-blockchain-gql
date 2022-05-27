using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class HeaderType : ObjectGraphType<Header>
{
    public HeaderType()
    {
        Field(x => x.Number);
        Field(x => x.ParentHash);
        Field(x => x.ExtrinsicsRoot);
        Field(x => x.StateRoot);
        Field(x => x.Digest);
        Field("Hash", x => Hex.GetString0X(x.ComputeHash()));
    }
}