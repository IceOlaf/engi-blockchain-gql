using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class JobsPagedQueryArguments : PagedQueryArguments
{
    public string? Creator { get; set; }

    public JobStatus? Status { get; set; }

    public string? Search { get; set; }

    public Language? Language { get; set; }

    public uint? MinFunding { get; set; }

    public uint? MaxFunding { get; set; }
}