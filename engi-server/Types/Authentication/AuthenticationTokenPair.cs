using System.Text.Json.Serialization;
using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Types.Authentication;

public class AuthenticationTokenPair
{
    public string AccessToken { get; set; } = null!;

    [JsonIgnore]
    public RefreshToken RefreshToken { get; set; } = null!;
}