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
        Field(x => x.Author, type: typeof(AddressGraphType))
            .Description("The address of the solution author.");
        Field(x => x.Awards, nullable: true, type: typeof(ListGraphType<AwardGraphType>))
            .Description("The awards list.");
        Field(x => x.PatchUrl)
            .Description("The URL of the patch.");
        Field(x => x.PullRequestUrl, nullable: true)
            .Description("The URL of the PR.");
        Field(x => x.Attempt, type: typeof(AttemptGraphType))
            .Description("The attempt that resulted in this solution.");
    }
}
