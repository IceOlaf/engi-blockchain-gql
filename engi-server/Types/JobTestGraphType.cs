using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobTestGraphType : ObjectGraphType<Test>
{
    public JobTestGraphType()
    {
        Field(x => x.Id);
        Field(x => x.Result);
        Field(x => x.Required);
    }
}