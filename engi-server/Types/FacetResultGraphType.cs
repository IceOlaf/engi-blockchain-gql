using GraphQL.Types;
using Raven.Client.Documents.Queries.Facets;

namespace Engi.Substrate.Server.Types;

public class FacetResultGraphType<TValueGraphType> : ObjectGraphType<FacetResult>
    where TValueGraphType : IGraphType
{
    public FacetResultGraphType()
    {
        Field(x => x.Name);
        Field(x => x.Values, type: typeof(ListGraphType<TValueGraphType>));
    }
}

public class FacetResultGraphType : FacetResultGraphType<FacetValueGraphType>
{
}
