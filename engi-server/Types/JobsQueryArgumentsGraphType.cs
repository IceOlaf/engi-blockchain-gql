using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobsQueryArgumentsGraphType : OrderedQueryArgumentsGraphType<JobsQueryArguments, JobsOrderByProperty>
{
    public JobsQueryArgumentsGraphType()
    {
        Description = "The paged query arguments for the jobs query.";

        Field(x => x.Creator, nullable: true, type: typeof(ListGraphType<StringGraphType>))
            .Description("Filter by one or more creator addresses.");

        Field(x => x.CreatedAfter, nullable: true)
            .Description("Filter by the time the job was created.");

        Field(x => x.Status, nullable: true)
            .Description("Filter by job status.");

        Field(x => x.Search, nullable: true)
            .Description("Search terms to query for in; fields indexed: chain id, name, repository. The query is treated as separated words and the last word is treated as `startsWith`.");

        Field(x => x.Language, nullable: true, type: typeof(ListGraphType<EnumerationGraphType<Language>>))
            .Description("Filter by one or more job languages.");

        Field(x => x.MinFunding, nullable: true, type: typeof(UInt128GraphType))
            .Description("The minimum funding to search for (inclusive).");

        Field(x => x.MaxFunding, nullable: true, type: typeof(UInt128GraphType))
            .Description("The maximum funding to search for (inclusive).");

        Field(x => x.RepositoryFullName, nullable: true)
            .Description("The repository fullname to search for. e.g. for https://github.com/engi-network/blockchain, 'engi-network/blockchain'.");

        Field(x => x.RepositoryOrganization, nullable: true)
            .Description("The repository organization to search for. e.g. for https://github.com/engi-network/blockchain, 'engi-network'.");
    }
}

