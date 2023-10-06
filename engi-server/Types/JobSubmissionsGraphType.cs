using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobSubmissionsGraphType : ObjectGraphType<JobSubmissionsDetails>
{
    public JobSubmissionsGraphType()
    {
        Field(x => x.Status)
            .Description("Submission status.");

        Field(x => x.AttemptCreated)
            .Description("Date of attempt creatiion");

        Field(x => x.UserInfo, type: typeof(UserInfoGraphType))
            .Description("User info of attempter.");

        Field(x => x.AttemptId, type: typeof(IdGraphType))
            .Description("Attempt id.");

        Field(x => x.Attempt, type: typeof(AttemptStageGraphType))
            .Description("Attempt stage status.");

        Field(x => x.Solve, type: typeof(SolveStageGraphType))
            .Description("Solve stage status.");
    }
}
