using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Octokit;

namespace Engi.Substrate.Server.Github;

public class GithubClientFactory
{
    private readonly IMemoryCache cache;
    private readonly EngiOptions options;

    public GithubClientFactory(
        IMemoryCache cache,
        IOptions<EngiOptions> options)
    {
        this.cache = cache;
        this.options = options.Value;
    }

    public GitHubClient Create()
    {
        var jwtToken = cache.GetOrCreate("github-jwt", e =>
        {
            var generator = new GitHubJwt.GitHubJwtFactory(
                new Base64PrivateKeySource(options.GithubAppPrivateKey),
                new GitHubJwt.GitHubJwtFactoryOptions
                {
                    AppIntegrationId = options.GithubAppId,
                    ExpirationSeconds = 600
                }
            );

            var jwtToken = generator.CreateEncodedJwtToken();

            var decoded = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken);

            e.AbsoluteExpiration = decoded.ValidTo;

            return jwtToken;
        });

        return new GitHubClient(new ProductHeaderValue("engi-bot"))
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };
    }

    public GitHubClient CreateAnonymous()
    {
        return new GitHubClient(new ProductHeaderValue("engi-bot"));
    }
    
    public async Task<GitHubClient> SpecializeForAsync(GitHubClient octokit, long installationId)
    {
        var token = await cache.GetOrCreateAsync($"github-app-token-{installationId}", async e =>
        {
            var token = await octokit.GitHubApps.CreateInstallationToken(installationId);

            e.AbsoluteExpiration = token.ExpiresAt;

            return token.Token;
        });

        return new GitHubClient(octokit.Connection)
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<GitHubClient> CreateForAsync(long installationId)
    {
        var octokit = Create();

        return await SpecializeForAsync(octokit, installationId);
    }
}