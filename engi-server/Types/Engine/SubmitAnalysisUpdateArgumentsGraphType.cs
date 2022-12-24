using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Engine;

public class SubmitAnalysisUpdateArgumentsGraphType : InputObjectGraphType<SubmitAnalysisUpdateArguments>
{
    public SubmitAnalysisUpdateArgumentsGraphType()
    {
        Description = "Arguments for submitting an analysis update.";

        Field(x => x.Id)
            .Description("The id of the analysis.");

        Field(x => x.Stdout)
            .Description("The standard output stream output.");

        Field(x => x.Stderr)
            .Description("The standard error stream output.");

        Field(x => x.ReturnCode)
            .Description("The command's execution return code.");
    }
}
