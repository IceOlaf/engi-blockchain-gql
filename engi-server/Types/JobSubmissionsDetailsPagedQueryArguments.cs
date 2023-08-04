using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Jobs;
using Engi.Substrate.Server.Types.Validation;

namespace Engi.Substrate.Server.Types;

public class JobSubmissionsDetailsPagedQueryArguments : PagedQueryArguments
{
    [Required]
    public ulong JobId { get; set; }
}
