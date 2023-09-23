using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class ListDraftsArgumentsGraphType : InputObjectGraphType<ListDraftsArguments>
{
    public ListDraftsArgumentsGraphType()
    {
        Field(x => x.Skip)//, type: typeof(UInt64GraphType))
            .Description("Skip.");

        Field(x => x.Take)//, type: typeof(UInt64GraphType))
            .Description("Take.");
    }
}
