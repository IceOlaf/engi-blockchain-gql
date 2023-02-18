using GraphQL.Types;
using Raven.Client.Documents.Queries.Facets;

namespace Engi.Substrate.Server.Types;

public class FacetValueGraphType : ObjectGraphType<FacetValue>
{
    public FacetValueGraphType()
    {
        Field(x => x.Range);
        Field(x => x.Count);
    }
}

public class FacetValueExtended : FacetValue
{
    public string Value { get; set; } = null!;
}

public class FacetValueExtendedGraphType : ObjectGraphType<FacetValueExtended>
{
    public FacetValueExtendedGraphType()
    {
        Field(x => x.Range);
        Field(x => x.Count);
        Field(x => x.Value);
    }
}
