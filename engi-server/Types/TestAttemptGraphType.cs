using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class TestAttemptGraphType : ObjectGraphType<TestAttempt>
{
    public TestAttemptGraphType()
    {
        Description = "A test attempt.";

        Field(x => x.Id)
            .Description("The test id.");
        Field(x => x.Result)
            .Description("The test result.");
        Field(x => x.FailedResultMessage, nullable: true)
            .Description("For a failed test, the failure message.");
    }
}