using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types;

public class PagedQueryArguments
{
    [Range(0, int.MaxValue)]
    public int Skip { get; set; } = 0;

    [Range(10, 100)]
    public int Limit { get; set; } = 25;
}