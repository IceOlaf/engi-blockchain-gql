using Engi.Substrate.Server.Types;

namespace Engi.Substrate;

public class OrderedQueryArguments<T> : PagedQueryArguments
    where T : Enum
{
    public T OrderByProperty { get; set; } = default!;

    public OrderByDirection OrderByDirection { get; set; }
}