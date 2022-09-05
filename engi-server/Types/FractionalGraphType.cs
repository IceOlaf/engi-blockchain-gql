using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class FractionalGraphType : ObjectGraphType<Fractional>
{
    public FractionalGraphType()
    {
        Field(x => x.Numerator);
        Field(x => x.Denominator);
    }
}