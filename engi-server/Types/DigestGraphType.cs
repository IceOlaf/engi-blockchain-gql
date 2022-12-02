using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class DigestGraphType : ObjectGraphType<Digest>
{
    public DigestGraphType()
    {
        Field(x => x.Logs);
    }
}