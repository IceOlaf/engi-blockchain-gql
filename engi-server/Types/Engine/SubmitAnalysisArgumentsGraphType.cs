using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Engine;

public class SubmitAnalysisArgumentsGraphType : InputObjectGraphType<SubmitAnalysisArguments>
{
    public SubmitAnalysisArgumentsGraphType()
    {
        Field(x => x.Url)
            .Description("The repository URL. Only Github repositories currently supported.");

        Field(x => x.Branch)
            .Description("The branch to analyze.");

        Field(x => x.Commit)
            .Description("The commit hash to analyze.");
    }
}
