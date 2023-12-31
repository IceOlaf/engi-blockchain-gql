using Raven.Client.Documents.Queries.Facets;

namespace Engi.Substrate.Server.Types;

public class FacetResults
{
    public FacetResult CreatedOnPeriod { get; set; } = null!;

    public FacetResult Technologies { get; set; } = null!;

    public FacetResult Repositories { get; set; } = null!;

    public FacetResult Organizations { get; set; } = null!;
}
