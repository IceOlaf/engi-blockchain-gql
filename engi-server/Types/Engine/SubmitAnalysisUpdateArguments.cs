using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Engine;

public class SubmitAnalysisUpdateArguments
{
    [Required]
    public string Id { get; set; } = null!;

    [Required(AllowEmptyStrings = true)]
    public string Stdout { get; set; } = null!;

    [Required(AllowEmptyStrings = true)]
    public string Stderr { get; set; } = null!;

    public int ReturnCode { get; set; }
}
