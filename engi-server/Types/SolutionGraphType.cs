using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class SolutionGraphType : ObjectGraphType<Solution>
{
    public SolutionGraphType()
    {
        Description = "A solution to an ENGI job.";

        // TODO: https://github.com/graphql-dotnet/graphql-dotnet/issues/3303
        Field(x => x.SolutionId, type: typeof(IdGraphType))
            .Description("The id of the solution on the chain.");
        Field(x => x.JobId, type: typeof(IdGraphType))
            .Description("The id of the job related to this solution.");
        Field(x => x.Author)
            .Description("The address of the solution author.");
        Field(x => x.PatchUrl)
            .Description("The URL of the patch.");
        Field(x => x.Attempt, type: typeof(AttemptGraphType))
            .Description("The attempt that resulted in this solution.");
    }
}