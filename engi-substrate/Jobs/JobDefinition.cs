using System.Numerics;

namespace Engi.Substrate.Jobs;

public class JobDefinition
{
    public BigInteger Funding { get; init; }

    public Language Language { get; init; }

    public string RepositoryUrl { get; init; } = null!;

    public string BranchName { get; init; } = null!;

    public string CommitHash { get; init; } = null!;

    public Test[] Tests { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string[] FilesRequirement { get; init; } = null!;
}