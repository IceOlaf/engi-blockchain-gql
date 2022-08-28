namespace Engi.Substrate.Server.Types;

public class OrderedQueryArguments<T> : PagedQueryArguments
    where T : Enum
{
    public T OrderByProperty { get; set; } = default!;

    public OrderByDirection OrderByDirection { get; set; }
}