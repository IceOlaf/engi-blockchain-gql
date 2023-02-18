using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types;

public class ActivityArguments
{
    [Range(1, 25)]
    public int DayCount { get; set; } = 15;

    [Range(1, 25)]
    public int MaxCompletedCount { get; set; } = 10;

    [Range(1, 25)]
    public int MaxNotCompletedCount { get; set; } = 10;
}
