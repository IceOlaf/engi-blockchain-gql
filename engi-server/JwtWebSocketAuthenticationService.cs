using GraphQL.Server.Transports.AspNetCore.WebSockets;
using GraphQL.Transport;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Engi.Substrate.Server;

public class JwtWebSocketAuthenticationService : IWebSocketAuthenticationService
{
    private readonly TokenValidationParameters tokenValidationParameters;

    public JwtWebSocketAuthenticationService(
        TokenValidationParameters tokenValidationParameters)
    {
        this.tokenValidationParameters = tokenValidationParameters;
    }

    public Task AuthenticateAsync(IWebSocketConnection connection, string subProtocol, OperationMessage operationMessage)
    {
        try
        {
            // for connections authenticated via HTTP headers, no need to reauthenticate

            if (connection.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                return Task.CompletedTask;
            }

            // attempt to read the 'Authorization' key from the payload object and verify it contains "Bearer: XXXXXXXX"

            if (operationMessage.Payload is JsonElement json
                && json.TryGetProperty("authorization", out var authorization))
            {
                string? value = authorization.GetString();

                if (value?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                {
                    string token = value.Substring(7);

                    var handler = new JwtSecurityTokenHandler();

                    // this will throw and will be caught above

                    connection.HttpContext.User = handler.ValidateToken(token, tokenValidationParameters, out _);
                }
            }
        }
        catch
        {
            // ignore
        }

        return Task.CompletedTask;
    }
}
