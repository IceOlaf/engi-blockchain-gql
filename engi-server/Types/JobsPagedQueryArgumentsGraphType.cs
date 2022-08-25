namespace Engi.Substrate.Server.Types;

public class JobsPagedQueryArgumentsGraphType : PagedQueryArgumentsGraphType<JobsPagedQueryArguments>
{
    public JobsPagedQueryArgumentsGraphType()
    {
        Description = "The paged query arguments for the jobs query.";

        Field(x => x.Search)
            .Description("Search terms to query for in; fields indexed: chain id, name, repository. The query is treated as separated words and the last word is treated as `startsWith`.");
    }
}