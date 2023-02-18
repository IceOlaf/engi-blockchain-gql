using Raven.Client.Documents.Queries.Facets;

namespace Engi.Substrate.Server.Types;

public class FacetResults
{
    public FacetResult CreatedOnPeriod { get; set; } = null!;

    public FacetResult Language { get; set; } = null!;

    public FacetResult Organization { get; set; } = null!;
}
