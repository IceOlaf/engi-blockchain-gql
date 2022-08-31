using System.Numerics;
using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class JobsQueryArguments : OrderedQueryArguments<JobsOrderByProperty>
{
    public string? Creator { get; set; }

    public JobStatus? Status { get; set; }

    public string? Search { get; set; }

    public Language? Language { get; set; }

    public BigInteger? MinFunding { get; set; }

    public BigInteger? MaxFunding { get; set; }
}