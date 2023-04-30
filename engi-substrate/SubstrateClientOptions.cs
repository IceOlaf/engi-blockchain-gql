using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate;

public class SubstrateClientOptions
{
    [Required]
    public string HttpUrl { get; set; } = null!;

    [Required]
    public string WsUrl { get; set; } = null!;
}
