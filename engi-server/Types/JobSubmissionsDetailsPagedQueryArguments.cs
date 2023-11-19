using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types;

public class JobSubmissionsDetailsPagedQueryArguments : PagedQueryArguments
{
    [Required]
    public ulong JobId { get; set; }
}
