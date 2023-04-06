namespace Engi.Substrate.Jobs;

public class EngineAnalysisResult
{
    public Technology[] Technologies { get; set; } = Array.Empty<Technology>();

    public string[]? Files { get; set; }

    public RepositoryComplexity? Complexity { get; set; }

    public TestAttempt[]? Tests { get; set; }
}
