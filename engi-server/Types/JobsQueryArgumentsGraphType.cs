namespace Engi.Substrate.Server.Types;

public class JobsQueryArgumentsGraphType : OrderedQueryArgumentsGraphType<JobsQueryArguments, JobsOrderByProperty>
{
    public JobsQueryArgumentsGraphType()
    {
        Description = "The paged query arguments for the jobs query.";

        Field(x => x.Creator, nullable: true)
            .Description("Filter by the creator's address.");

        Field(x => x.Status, nullable: true)
            .Description("Filter by job status.");

        Field(x => x.Search, nullable: true)
            .Description("Search terms to query for in; fields indexed: chain id, name, repository. The query is treated as separated words and the last word is treated as `startsWith`.");

        Field(x => x.Language, nullable: true)
            .Description("Job language to search for.");

        Field(x => x.MinFunding, nullable: true)
            .Description("The minimum funding to search for (inclusive). The value is currently bound to 32-bits.");

        Field(x => x.MaxFunding, nullable: true)
            .Description("The maximum funding to search for (inclusive). The value is currently bound to 32-bits.");
    }
}