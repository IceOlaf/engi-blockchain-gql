using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class PagedResultGraphType<TGraphType, T> : ObjectGraphType<PagedResult<T>> 
    where TGraphType : ObjectGraphType<T>
{
    public PagedResultGraphType()
    {
        Description = "An object representing a page of a query and its total count.";

        Field(x => x.Items, type: typeof(ListGraphType<TGraphType>))
            .Description("The items included in this page of the results.");
        Field(x => x.TotalCount)
            .Description("The total count of the results.");
    }
}