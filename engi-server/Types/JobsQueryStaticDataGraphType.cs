using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobsQueryStaticDataGraphType : ObjectGraphType<JobsQueryStaticData>
{
    public JobsQueryStaticDataGraphType()
    {
        Field(x => x.Languages)
            .Description("All the languages available to query.");
    }
}
