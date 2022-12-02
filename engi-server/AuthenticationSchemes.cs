using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Engi.Substrate.Server;

public class AuthenticationSchemes
{
    public const string Jwt = JwtBearerDefaults.AuthenticationScheme;
    public const string ApiKey = "API Key";
}