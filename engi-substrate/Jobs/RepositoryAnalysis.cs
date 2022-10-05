namespace Engi.Substrate.Jobs;

public enum RepositoryAnalysisStatus
{
    Created,
    Queued,
    Started,
    Completed
}

public class RepositoryAnalysis
{
    public string Id { get; set; } = null!;
    public string RepositoryUrl { get; set; } = null!;
    public string Branch { get; set; } = null!;
    public string Commit { get; set; } = null!;
    public RepositoryAnalysisStatus Status { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}