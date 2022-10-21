using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class DistributeCompletedJobCodeArgumentsGraphType : InputObjectGraphType<DistributeCompletedJobCodeArguments>
{
    public DistributeCompletedJobCodeArgumentsGraphType()
    {
        Description = "The arguments for distributing a job's code to the source repository.";

        Field(x => x.JobId, type: typeof(UInt64GraphType))
            .Description("The job id.");
        Field(x => x.SolutionId, type: typeof(UInt64GraphType))
            .Description("The solution id.");
    }
}