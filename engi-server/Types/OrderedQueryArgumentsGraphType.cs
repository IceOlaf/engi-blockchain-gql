namespace Engi.Substrate.Server.Types;

public abstract class OrderedQueryArgumentsGraphType<TArguments, TOrderByProperty> : PagedQueryArgumentsGraphType<TArguments> 
    where TArguments : OrderedQueryArguments<TOrderByProperty> 
    where TOrderByProperty : Enum
{
    protected OrderedQueryArgumentsGraphType()
    {
        Field(x => x.OrderByProperty, nullable: true)
            .Description("Property to order by. Defaults to the first property in the implementation type.");

        Field(x => x.OrderByDirection, nullable: true)
            .Description("Whether to sort in ascending or descending order. Defaults to ascending.");
    }
}