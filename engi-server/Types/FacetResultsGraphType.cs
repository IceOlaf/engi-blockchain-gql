using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class FacetResultsGraphType : ObjectGraphType<FacetResults>
{
    public FacetResultsGraphType()
    {
        Field(x => x.CreatedOnPeriod, type: typeof(FacetResultGraphType<FacetValueExtendedGraphType>));
        Field(x => x.Technologies, type: typeof(FacetResultGraphType));
        Field(x => x.Organization, type: typeof(FacetResultGraphType));
    }
}
