namespace Engi.Substrate.Server.Types;

public class JobsPagedQueryArgumentsGraphType : PagedQueryArgumentsGraphType<JobsPagedQueryArguments>
{
    public JobsPagedQueryArgumentsGraphType()
    {
        Description = "The paged query arguments for the jobs query.";
    }
}