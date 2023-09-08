using System.Security.Claims;

namespace Engi.Substrate.Server.Types.Authentication;

public class GraphQLUserContext : Dictionary<string, object?>
{
    public ClaimsPrincipal User { get; set; }
}
