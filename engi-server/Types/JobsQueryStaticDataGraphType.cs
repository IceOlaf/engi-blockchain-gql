using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobsQueryStaticDataGraphType : ObjectGraphType<JobsQueryStaticData>
{
    public JobsQueryStaticDataGraphType()
    {
        Field(x => x.Technologies)
            .Description("All the technologies available to query.");
    }
}
