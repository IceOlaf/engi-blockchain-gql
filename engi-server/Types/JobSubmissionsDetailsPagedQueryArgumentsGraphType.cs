namespace Engi.Substrate.Server.Types;

public class JobSubmissionsDetailsPagedQueryArgumentsGraphType : PagedQueryArgumentsGraphType<JobSubmissionsDetailsPagedQueryArguments>
{
    public JobSubmissionsDetailsPagedQueryArgumentsGraphType()
    {
        Description = "The paged query arguments for the transactions query.";

        Field(x => x.JobId)
            .Description("The job id to query for.");
    }
}
