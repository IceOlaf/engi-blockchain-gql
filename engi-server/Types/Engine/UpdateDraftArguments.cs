using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Engine;

public class UpdateDraftArguments
{
    public string Id { get; set; } = null!;

    public string? IsAddable { get; set; } = null!;

    public string? IsEditable { get; set; } = null!;

    public string? IsDeletable { get; set; } = null!;

    public ulong? Funding { get; set; }

    public string[]? Tests { get; set; } = null!;

    public string? Name { get; set; } = null!;
}
