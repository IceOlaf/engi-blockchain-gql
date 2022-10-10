namespace Engi.Substrate.Jobs;

public class RepositoryAnalysis
{
    public string Id { get; set; } = null!;
    
    public string RepositoryUrl { get; set; } = null!;

    public string Branch { get; set; } = null!;
    
    public string Commit { get; set; } = null!;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = null!;

    public RepositoryAnalysisStatus Status { get; set; }

    public Language? Language { get; set; }

    public string[]? Files { get; set; }
    
    public RepositoryComplexity? Complexity { get; set; }

    public TestAttempt[]? Tests { get; set; }

    public CommandLineExecutionResult? ExecutionResult { get; set; }
}