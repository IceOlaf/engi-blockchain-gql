using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Engine;

public class CreateDraftArguments
{
    [Required, HttpUrl]
    public string Url { get; set; } = null!;

    [Required]
    public string Branch { get; set; } = null!;

    [Required]
    public string Commit { get; set; } = null!;
}
