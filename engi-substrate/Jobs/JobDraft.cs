using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engi.Substrate.Jobs;

public class JobDraft : IDispatched
{
    public string Id { get; set; } = null!;

    public bool Completed { get; set; } = false;

    public string CreatedBy { get; set; } = null!;

    public string[]? Tests { get; set; } = null;

    public string? IsEditable { get; set; } = null;

    public string? IsAddable { get; set; } = null;

    public string? IsDeletable { get; set; } = null;

    public ulong? Funding { get; set; } = null;

    public string? Name { get; set; } = null;

    public string AnalysisId { get; set; } = null!;

    public DateTime? DispatchedOn { get; set; }
}
