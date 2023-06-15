using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AttemptStageGraphType : ObjectGraphType<AttemptStage>
{
    public AttemptStageGraphType()
    {
        Field(x => x.Status)
            .Description("Attempt status.");

        Field(x => x.Results, type: typeof(CommandLineExecutionResultGraphType), nullable: true)
            .Description("Attempt stage results.");
    }
}
