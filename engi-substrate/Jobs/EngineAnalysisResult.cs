namespace Engi.Substrate.Jobs;

public class EngineAnalysisResult
{
    public Language Language { get; set; }

    public string[]? Files { get; set; }

    public RepositoryComplexity? Complexity { get; set; }

    public TestAttempt[]? Tests { get; set; }
}
