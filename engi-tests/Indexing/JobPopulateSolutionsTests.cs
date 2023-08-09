using System;
using Engi.Substrate.Jobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Engi.Substrate.Indexing;

public class JobPopulateSolutionsTests
{
    private static readonly Address Address1 = "5EUJ3p7ds1436scqdA2n6ph9xVs6chshRP1ADjgK1Qj3Hqs2";
    private static readonly Address Address2 = "5EyiGwuYPgGiEfwpPwXyH5TwXXEUFz6ZgPhzYik2fMCcbqMC";

    private Job CreateJob()
    {
        var mockLogger = new Mock<ILogger>();
        var job = new Job
        {
            Tests = new[]
            {
                Test(1, required: true),
                Test(2, required: true),
                Test(3, required: true)
            }
        };

        job.PopulateSolutions(Address1, new[]
        {
            new SolutionSnapshot
            {
                SolutionId = 1,
                Author = Address1,
                Attempt = new()
                {
                    Tests = new []
                    {
                        Attempt(1, TestResult.Passed),
                        Attempt(2, TestResult.Passed),
                        Attempt(3, TestResult.Failed)
                    }
                },
                SnapshotOn = new BlockReference
                {
                    DateTime = new DateTime(2022, 9, 6, 0, 0, 0)
                }
            },

            new SolutionSnapshot
            {
                SolutionId = 2,
                Author = Address1,
                Attempt = new()
                {
                    Tests = new []
                    {
                        Attempt(1, TestResult.Passed),
                        Attempt(2, TestResult.Failed),
                        Attempt(3, TestResult.Failed)
                    }
                },
                SnapshotOn = new BlockReference
                {
                    DateTime = new DateTime(2022, 9, 6, 1, 0, 0)
                }
            },

            new SolutionSnapshot
            {
                SolutionId = 3,
                Author = Address2,
                Attempt = new()
                {
                    Tests = new []
                    {
                        Attempt(1, TestResult.Passed),
                        Attempt(2, TestResult.Passed),
                        Attempt(3, TestResult.Passed)
                    }
                },
                SnapshotOn = new BlockReference
                {
                    DateTime = new DateTime(2022, 9, 6, 2, 0, 0)
                }
            }
        }, mockLogger.Object);

        return job;
    }

    [Fact]
    public void LeadingSolution()
    {
        var job = CreateJob();

        Assert.Equal(Address2, job.LeadingSolution!.Author);
        Assert.Equal(3UL, job.LeadingSolution!.SolutionId);
    }

    [Fact]
    public void CurrentUserSolution()
    {
        var job = CreateJob();

        Assert.Equal(Address1, job.CurrentUserSolution!.Author);
        Assert.Equal(1UL, job.CurrentUserSolution!.SolutionId);
    }

    [Fact]
    public void AverageProgress()
    {
        var job = CreateJob();

        Assert.Equal(2, job.AverageProgress!.Numerator);
        Assert.Equal(3, job.AverageProgress!.Denominator);
    }

    [Fact]
    public void IgnoresNotRequiredTests()
    {
        var mockLogger = new Mock<ILogger>();
        var jobWithSomeTestsNotRequired = new Job
        {
            Tests = new[]
             {
                Test(1, required: true),
                Test(2, required: true),
                Test(3, required: false)
            }
        };

        jobWithSomeTestsNotRequired.PopulateSolutions(Address1, new[]
        {
            new SolutionSnapshot
            {
                SolutionId = 1,
                Author = Address1,
                Attempt = new()
                {
                    Tests = new[]
                    {
                        Attempt(1, TestResult.Passed),
                        Attempt(2, TestResult.Failed),
                        Attempt(3, TestResult.Passed)
                    }
                },
                SnapshotOn = new BlockReference
                {
                    DateTime = new DateTime(2022, 9, 6, 0, 0, 0)
                }
            }
        }, mockLogger.Object);

        Assert.Equal(1UL, jobWithSomeTestsNotRequired.LeadingSolution!.SolutionId);
        Assert.Equal(1UL, jobWithSomeTestsNotRequired.CurrentUserSolution!.SolutionId);
        Assert.Equal(1, jobWithSomeTestsNotRequired.AverageProgress!.Numerator);
    }

    private Test Test(int id,
        TestResult analysisResult = TestResult.Passed,
        bool required = false)
    {
        return new()
        {
            Id = $"test-{id}",
            Result = analysisResult,
            Required = required
        };
    }

    private TestAttempt Attempt(int id,
        TestResult result)
    {
        return new()
        {
            Id = $"test-{id}",
            Result = result
        };
    }
}
