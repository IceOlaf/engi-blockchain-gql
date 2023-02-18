using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace Engi.Substrate.Jobs;

public class JobsQueryArguments : OrderedQueryArguments<JobsOrderByProperty>, IValidatableObject
{
    public string[]? Creator { get; set; }

    public DateTime? CreatedAfter { get; set; }

    public JobStatus? Status { get; set; }

    public string? Search { get; set; }

    public Language[]? Language { get; set; }

    public BigInteger? MinFunding { get; set; }

    public BigInteger? MaxFunding { get; set; }

    public string[]? SolvedBy { get; set; }

    public string? CreatedOrSolvedBy { get; set; }

    public string[]? RepositoryFullName { get; set; }

    public string[]? RepositoryOrganization { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatedOrSolvedBy != null && (Creator != null || SolvedBy != null))
        {
            yield return new ValidationResult(
                $"Cannot filter by {nameof(CreatedOrSolvedBy)} in combination with either of {nameof(Creator)} or {nameof(SolvedBy)}");
        }
    }
}
