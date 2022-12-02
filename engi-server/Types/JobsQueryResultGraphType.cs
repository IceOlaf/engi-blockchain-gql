using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobsQueryResultGraphType : ObjectGraphType<JobsQueryResult>
{
    public JobsQueryResultGraphType()
    {
        Field(x => x.Result, type: typeof(PagedResultGraphType<JobGraphType, Job>))
            .Description("The paged results of the query.");
        Field(x => x.Suggestions, nullable: true)
            .Description("If search text is defined, suggestions for other searches.");
    }
}