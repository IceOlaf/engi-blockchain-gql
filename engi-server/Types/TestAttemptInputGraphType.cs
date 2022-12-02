using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class TestAttemptInputGraphType : InputObjectGraphType<TestAttempt>
{
    public TestAttemptInputGraphType()
    {
        Description = "A test attempt.";

        Field(x => x.Id)
            .Description("The test id.");
        Field(x => x.Result, type: typeof(EnumerationGraphType<TestResult>))
            .Description("The test result.");
        Field(x => x.FailedResultMessage, nullable: true)
            .Description("For a failed test, the failure message.");
    }
}