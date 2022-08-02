using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class PagedResultGraphType<TGraphType, T> : ObjectGraphType<PagedResult<T>> 
    where TGraphType : ObjectGraphType<T>
{
    public PagedResultGraphType()
    {
        Field(x => x.Items, type: typeof(ListGraphType<TGraphType>));
        Field(x => x.TotalCount);
    }
}