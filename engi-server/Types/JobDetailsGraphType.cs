using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobDetailsGraphType : ObjectGraphType<JobDetails>
{
    public JobDetailsGraphType()
    {
        Field(x => x.Job, type: typeof(JobGraphType))
            .Description("The job.");

        Field(x => x.CreatorUserInfo, type: typeof(UserInfoGraphType))
            .Description("User information about the creator of the job.");
    }
}
