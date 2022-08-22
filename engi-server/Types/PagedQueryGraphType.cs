using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class PagedQueryArgumentsGraphType<T> : InputObjectGraphType<T>
    where T : PagedQueryArguments
{
    public PagedQueryArgumentsGraphType()
    {
        Field(x => x.Skip)
            .DefaultValue(0)
            .Description("Number of items to skip from the matches. Used for paging.");

        Field(x => x.Limit)
            .DefaultValue(25)
            .Description("Maximum number of items to return. Used for paging. Minimum = 25, Maximum = 100.");
    }
}
