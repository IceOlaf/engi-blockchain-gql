using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class RepositoryComplexityGraphType : ObjectGraphType<RepositoryComplexity>
{
    public RepositoryComplexityGraphType()
    {
        Description = "Repository Complexity";

        Field(x => x.SLOC)
            .Description("SLOC count.");
        Field(x => x.Cyclomatic)
            .Description("Cyclomatic Complexity.");
    }
}
