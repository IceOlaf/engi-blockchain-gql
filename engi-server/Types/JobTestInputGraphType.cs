using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobTestInputGraphType : InputObjectGraphType<Test>
{
    public JobTestInputGraphType()
    {
        Description = "An input type for the Test definition part of job creation.";

        Field(x => x.Id)
            .Description("The identifier of the test extracted in analysis.");
        Field(x => x.Result)
            .Description("The result of the test at analysis.");
        Field(x => x.Required)
            .Description("Is a result required to complete the job?");
    }
}