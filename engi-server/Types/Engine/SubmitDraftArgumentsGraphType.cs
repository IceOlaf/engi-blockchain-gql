using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Engine;

public class SubmitDraftArgumentsGraphType : InputObjectGraphType<SubmitDraftArguments>
{
    public SubmitDraftArgumentsGraphType()
    {
        Field(x => x.Url)
            .Description("The repository URL. Only Github repositories currently supported.");

        Field(x => x.Branch)
            .Description("The branch to analyze.");

        Field(x => x.Commit)
            .Description("The commit hash to analyze.");

        Field(x => x.IsAddable)
            .Description("The glob pattern that is addable.");

        Field(x => x.IsEditable)
            .Description("The glob pattern that is editable.");

        Field(x => x.IsDeletable)
            .Description("The glob pattern that is deletable.");

        Field(x => x.Funding)
            .Description("The funding amount.");

        Field(x => x.Name)
            .Description("The name.");

        Field(x => x.Tests, type: typeof(ListGraphType<StringGraphType>))
            .Description("Passing tests.");
    }
}
