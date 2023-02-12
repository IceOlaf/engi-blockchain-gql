using System.Numerics;

namespace Engi.Substrate.Jobs;

public class JobsQueryArguments : OrderedQueryArguments<JobsOrderByProperty>
{
    public string[]? Creator { get; set; }

    public DateTime? CreatedAfter { get; set; }

    public JobStatus? Status { get; set; }

    public string? Search { get; set; }

    public Language[]? Language { get; set; }

    public BigInteger? MinFunding { get; set; }

    public BigInteger? MaxFunding { get; set; }

    public string[]? SolvedBy { get; set; }

    public string[]? RepositoryFullName { get; set; }

    public string[]? RepositoryOrganization { get; set; }
}
