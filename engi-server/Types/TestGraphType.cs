using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class TestGraphType : ObjectGraphType<Test>
{
    public TestGraphType()
    {
        Description = "A description of the current vs desired state of a test in an ENGI job.";

        Field(x => x.Id)
            .Description("The test id.");
        Field(x => x.Result)
            .Description("The result of the test.");
        Field(x => x.ResultMessage)
            .Description("The message associated with the result of the test.");
        Field(x => x.Required)
            .Description("The required result of the test.");
        Field(x => x.RequiredMessage)
            .Description("The message associated with the required result of the test.");
    }
}