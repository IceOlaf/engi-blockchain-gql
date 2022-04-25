using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class DigestType : ObjectGraphType<Digest>
{
    public DigestType()
    {
        Field(x => x.Logs);
    }
}