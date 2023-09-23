using System.Text.Json;
using System.Text.Json.Serialization;

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

    public string? FailedReason { get; set; } = null;

    public DateTime? ProcessedOn { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public string DirectoryEntries {
        get {
            if (this.Files != null)
            {
                var result = DirectoryEntry.DirectoryEntries(this.Files);
                return JsonSerializer.Serialize(result);
            }
            else
            {
                return JsonSerializer.Serialize(new List<DirectoryEntry>());
            }
        }
        protected set { }
    }
}
