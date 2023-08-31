namespace Engi.Substrate.Jobs;

public class RepositoryAnalysis : IDispatched
{
    public string Id { get; set; } = null!;

    public ulong JobId { get; set; }

    public string RepositoryUrl { get; set; } = null!;

    public string Branch { get; set; } = null!;

    public string Commit { get; set; } = null!;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = null!;

    public DateTime? DispatchedOn { get; set; }

    public RepositoryAnalysisStatus Status { get; set; }

    public Technology[]? Technologies { get; set; }

    public string[]? Files { get; set; }

    public RepositoryComplexity? Complexity { get; set; }

    public TestAttempt[]? Tests { get; set; }

    public CommandLineExecutionResult? ExecutionResult { get; set; }

    public DateTime? ProcessedOn { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public List<DirectoryEntry> DirectoryEntries {
        get {
            if (this.Files != null)
            {
                return DirectoryEntry.DirectoryEntries(this.Files);
            }
            else
            {
                return new List<DirectoryEntry>();
            }
        }
        protected set { }
    }
}
