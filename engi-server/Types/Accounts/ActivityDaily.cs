using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class ActivityDaily
{
    public DateTime Date { get; init; }

    public IEnumerable<Job> Completed { get; init; } = null!;

    public IEnumerable<Job> NotCompleted { get; init; } = null!;
}
