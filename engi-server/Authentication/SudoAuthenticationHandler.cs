using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Engi.Substrate.Server.Authentication;

public class SudoAuthenticationHandler : AuthenticationHandler<SudoAuthenticationOptions>
{
    private readonly EngiOptions engiOptions;

    public SudoAuthenticationHandler(
        IOptionsMonitor<SudoAuthenticationOptions> options,
        IOptions<EngiOptions> engiOptions,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        this.engiOptions = engiOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        const string headerName = "X-API-KEY";

        var headers = Request.Headers;

        if (!headers.ContainsKey(headerName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (headers[headerName] != engiOptions.SudoApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API-KEY"));
        }

        var principal = new GenericPrincipal(
            new GenericIdentity("API"),
            new[] { "Sudo" });

        var ticket = new AuthenticationTicket(principal, "Sudo");

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}