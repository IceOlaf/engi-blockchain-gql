using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class SolutionResultsGraphType : ObjectGraphType<SolutionResult>
{
    public SolutionResultsGraphType()
    {
        Field(x => x.SolutionId, nullable: true)
            .Description("Solution ID.");

        Field(x => x.ResultHash, nullable: true)
            .Description("Hash of solution.");
    }
}
