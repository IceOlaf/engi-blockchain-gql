using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class JobsQueryResult
{
    public PagedResult<Job> Result { get; set; } = null!;

    public string[]? Suggestions { get; set; }
}