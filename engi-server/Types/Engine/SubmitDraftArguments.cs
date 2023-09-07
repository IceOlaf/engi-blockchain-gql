using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Engine;

public class SubmitDraftArguments
{
    [Required, HttpUrl]
    public string Url { get; set; } = null!;

    [Required]
    public string Branch { get; set; } = null!;

    [Required]
    public string Commit { get; set; } = null!;

    [Required]
    public string IsAddable { get; set; } = null!;

    [Required]
    public string IsEditable { get; set; } = null!;

    [Required]
    public string IsDeletable { get; set; } = null!;

    [Required]
    public ulong Funding { get; set; }

    [Required]
    public string[] Tests { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;
}
