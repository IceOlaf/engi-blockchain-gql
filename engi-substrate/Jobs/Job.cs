namespace Engi.Substrate.Jobs;

public class Job
{
    public ulong JobId { get; set; }

    public string Creator { get; init; } = null!;

    public string Funding { get; init; } = null!;

    public Repository Repository { get; init; } = null!;

    public Language Language { get; init; }

    public string Name { get; init; } = null!;

    public Test[] Tests { get; init; } = null!;

    public FilesRequirement Requirements { get; init; } = null!;

    public Solution? Solution { get; init; }

    public int AttemptCount { get; init; }

    public int SolutionUserCount { get; set; }

    public Solution? LeadingSolution { get; set; }

    public Solution? CurrentUserSolution { get; set; }

    public Fractional? AverageProgress { get; set; }

    public BlockReference CreatedOn { get; set; } = null!;

    public BlockReference UpdatedOn { get; set; } = null!;

    public JobStatus Status
    {
        get
        {
            if (Solution != null)
            {
                return JobStatus.Complete;
            }

            if (AttemptCount > 0)
            {
                return JobStatus.Active;
            }

            return JobStatus.Open;
        }
    }

    private int CountPassedTests(Solution solution)
    {
        return solution.Attempt.Tests.Count(submittedTest =>
        {
            var test = Tests.First(x => x.Id == submittedTest.Id);

            return test.Required
                && submittedTest.Result == TestResult.Passed;
        });
    }

    private Fractional? GetAverageProgress(ICollection<SolutionSnapshot> solutions)
    {
        if (!solutions.Any())
        {
            return null;
        }

        var bestPassedCountByAuthor = solutions
            .GroupBy(x => x.Author)
            .Select(x => x.Max(CountPassedTests))
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

    public void PopulateSolutions(
        Address? currentUser,
        ICollection<SolutionSnapshot> solutions)
    {
        LeadingSolution = solutions
            .OrderByDescending(CountPassedTests)
            .ThenBy(solution => solution.SnapshotOn.DateTime)
            .FirstOrDefault();

        if (currentUser != null)
        {
            CurrentUserSolution = solutions
                .Where(x => x.Author == currentUser)
                .MaxBy(CountPassedTests);
        }

        AverageProgress = GetAverageProgress(solutions);
    }
}