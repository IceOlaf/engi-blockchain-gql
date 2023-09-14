using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Engine;

public class UpdateDraftArgumentsGraphType : InputObjectGraphType<UpdateDraftArguments>
{
    public UpdateDraftArgumentsGraphType()
    {
        Field(x => x.Id)
            .Description("Draft ID to update.");

        Field(x => x.IsAddable, nullable: true)
            .Description("The glob pattern that is addable.");

        Field(x => x.IsEditable, nullable: true)
            .Description("The glob pattern that is editable.");

        Field(x => x.IsDeletable, nullable: true)
            .Description("The glob pattern that is deletable.");

        Field(x => x.Funding, nullable: true)
            .Description("The funding amount.");

        Field(x => x.Name, nullable: true)
            .Description("The name.");

        Field(x => x.Tests, nullable: true, type: typeof(ListGraphType<StringGraphType>))
            .Description("Passing tests.");
    }
}
