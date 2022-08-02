namespace Engi.Substrate.Server.Types;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }

    public long TotalCount { get; set; }
    
    public PagedResult(IEnumerable<T>? items, long totalCount)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        TotalCount = totalCount;
    }
}