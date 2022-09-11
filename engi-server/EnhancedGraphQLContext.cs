namespace Engi.Substrate.Server;

public class EnhancedGraphQLContext : Dictionary<string, object?>
{
    public IRequestCookieCollection Cookies { get; set; } = null!;
}