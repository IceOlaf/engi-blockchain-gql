using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobSubmissionsGraphType : ObjectGraphType<JobSubmissionsDetails>
{
    public JobSubmissionsGraphType()
    {
        Field(x => x.Status)
            .Description("Submission status.");

        Field(x => x.UserName)
            .Description("Username of attempter.");

        Field(x => x.Address, type: typeof(AddressGraphType))
            .Description("Attempter's address.");

        Field(x => x.ProfileImageUrl, nullable: true)
            .Description("Attempter's profile image url.");

        Field(x => x.AttemptId)
            .Description("Attempt id.");

        Field(x => x.Attempt, type: typeof(AttemptStageGraphType))
            .Description("Attempt stage status.");

        Field(x => x.Solve, type: typeof(SolveStageGraphType))
            .Description("Solve stage status.");
    }
}
