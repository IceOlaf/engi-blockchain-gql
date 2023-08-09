using Microsoft.Extensions.Logging;

namespace Engi.Substrate.Jobs;

public class Job
{
    public ulong JobId { get; set; }

    public string Creator { get; init; } = null!;

    public string Funding { get; init; } = null!;

    public Repository Repository { get; init; } = null!;

    public Technology[] Technologies { get; init; } = Array.Empty<Technology>();

    public string Name { get; init; } = null!;

    public Test[] Tests { get; init; } = null!;

    public FilesRequirement? Requirements { get; init; }

    public Solution? Solution { get; init; }

    public int AttemptCount { get; init; }

    public string[] SolvedBy { get; set; } = null!;

    public int SolutionUserCount { get; set; }

    public Solution? LeadingSolution { get; set; }

    public Solution? CurrentUserSolution { get; set; }

    public JobSubmissionsDetails[]? CurrentUserSubmissions { get; set; }

    public Fractional? AverageProgress { get; set; }

    public BlockReference CreatedOn { get; set; } = null!;

    public BlockReference UpdatedOn { get; set; } = null!;

    public JobStatus Status { get; set; }

    public RepositoryComplexity? Complexity { get; set; }

    private int CountPassedTests(Solution solution, ILogger logger)
    {
        return solution.Attempt.Tests.Count(submittedTest =>
        {
            try
            {
                var test = Tests.Single(x => x.Id == submittedTest.Id);

                return test.Required
                    && submittedTest.Result == TestResult.Passed;
            }
            catch (ArgumentNullException e)
            {
                logger.LogInformation(e, "SolutionId: {0} Tests are null.", solution.SolutionId);
                return false;
            }
            catch (InvalidOperationException e)
            {
                logger.LogInformation(e, "SolutionId: {0} Invalid operation.", solution.SolutionId);
                return false;
            }
        });
    }

    private Fractional? GetAverageProgress(ICollection<SolutionSnapshot> solutions, ILogger logger)
    {
        if (!solutions.Any())
        {
            return null;
        }

        var bestPassedCountByAuthor = solutions
            .GroupBy(x => x.Author)
            .Select(x => x.Max(x => CountPassedTests(x, logger)))
            .ToArray();

        Array.Sort(bestPassedCountByAuthor);

        int halfIndex = bestPassedCountByAuthor.Length / 2;

        int numerator = bestPassedCountByAuthor.Length % 2 == 0
            ? (int) Math.Round((bestPassedCountByAuthor[halfIndex - 1] + bestPassedCountByAuthor[halfIndex]) / 2m)
            : bestPassedCountByAuthor[halfIndex];

        return new()
        {
            Numerator = numerator,
            Denominator = Tests.Length
        };
    }

    public void PopulateSubmissions(ICollection<JobSubmissionsDetails> submissions)
    {
        CurrentUserSubmissions = submissions.ToArray();
    }

    public void PopulateSolutions(
        Address? currentUser,
        ICollection<SolutionSnapshot> solutions, ILogger logger)
    {
        LeadingSolution = solutions
            .OrderByDescending(x => CountPassedTests(x, logger))
            .ThenBy(solution => solution.SnapshotOn.DateTime)
            .FirstOrDefault();

        if (currentUser != null)
        {
            CurrentUserSolution = solutions
                .Where(x => x.Author.Equals(currentUser))
                .MaxBy(x => CountPassedTests(x, logger));
        }

        AverageProgress = GetAverageProgress(solutions, logger);
    }
}
