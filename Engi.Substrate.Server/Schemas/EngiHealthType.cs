using GraphQL.Types;

namespace Engi.Substrate.Server.Schemas;

public class EngiHealthType : ObjectGraphType<EngiHealth>
{
    public EngiHealthType()
    {
        Field(x => x.Chain);
        Field(x => x.NodeName);
        Field(x => x.Version);
        Field(x => x.Status);
    }
}