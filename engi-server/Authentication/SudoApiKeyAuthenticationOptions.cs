using Microsoft.AspNetCore.Authentication;

namespace Engi.Substrate.Server.Authentication;

public class SudoApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string? ApiKey { get; set; }
}

