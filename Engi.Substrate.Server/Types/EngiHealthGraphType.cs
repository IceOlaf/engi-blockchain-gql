using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiHealthGraphType : ObjectGraphType<EngiHealth>
{
    public EngiHealthGraphType()
    {
        Field(x => x.Chain);
        Field(x => x.NodeName);
        Field(x => x.Version);
        Field(x => x.Status);
    }
}
