using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Engi.Substrate.Server.Authentication;

public class SudoApiKeyAuthenticationHandler : AuthenticationHandler<SudoApiKeyAuthenticationOptions>
{
    public SudoApiKeyAuthenticationHandler(
        IOptionsMonitor<SudoApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        const string headerName = "X-API-KEY";

        var headers = Request.Headers;

        if (!headers.ContainsKey(headerName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (headers[headerName] != Options.ApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API-KEY"));
        }

        var principal = new GenericPrincipal(
            new GenericIdentity(AuthenticationSchemes.ApiKey),
            new[] { Roles.Sudo });

        var ticket = new AuthenticationTicket(principal, AuthenticationSchemes.ApiKey);

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}
