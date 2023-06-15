using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class SolveStageGraphType : ObjectGraphType<SolveStage>
{
    public SolveStageGraphType()
    {
        Field(x => x.Status)
            .Description("Solve status.");

        Field(x => x.Results, nullable: true, type: typeof(SolutionResultsGraphType))
            .Description("Solve stage results.");
    }
}
