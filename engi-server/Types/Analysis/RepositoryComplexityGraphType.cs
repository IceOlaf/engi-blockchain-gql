using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Analysis;

public class RepositoryComplexityGraphType : InputObjectGraphType<RepositoryComplexity>
{
    public RepositoryComplexityGraphType()
    {
        Description = "The computed complexity of a repository.";

        Field(x => x.SLOC)
            .Name("sloc")
            .Description("Source lines of code.");

        Field(x => x.Cyclomatic)
            .Description("The cyclomatic complexity.");
    }
}