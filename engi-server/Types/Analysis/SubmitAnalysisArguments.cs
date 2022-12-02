using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Analysis;

public class SubmitAnalysisArguments
{
    [Required]
    public string Url { get; set; } = null!;

    [Required]
    public string Branch { get; set; } = null!;

    [Required]
    public string Commit { get; set; } = null!;
}