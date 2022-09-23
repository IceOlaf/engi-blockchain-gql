using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Engi.Substrate.Identity;
using Engi.Substrate.Keys;
using Engi.Substrate.Server.Email;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using SessionOptions = Raven.Client.Documents.Session.SessionOptions;

namespace Engi.Substrate.Server.Types.Authentication;

public class AuthMutations : ObjectGraphType
{
    public AuthMutations()
    {
        this.AllowAnonymous();

        Field<AuthenticationTokenPairGraphType>("login")
            .Argument<NonNullGraphType<LoginArgumentsGraphType>>("args")
            .ResolveAsync(LoginAsync);

        Field<IdGraphType>("refresh")
            .ResolveAsync(RefreshAsync);

        Field<IdGraphType>("register")
            .Argument<NonNullGraphType<CreateUserArgumentsGraphType>>("user")
            .ResolveAsync(RegisterAsync);

        Field<IdGraphType>("confirmEmail")
            .Argument<NonNullGraphType<ConfirmEmailArgumentsGraphType>>("args")
            .ResolveAsync(ConfirmEmailAsync);
    }

    private async Task<object?> ConfirmEmailAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<ConfirmEmailArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider
            .GetRequiredService<IAsyncDocumentSession>();

        string userId = User.KeyFrom(args.Address);

        var user = await session.LoadAsync<User>(userId);

        if (user is not { EmailConfirmedOn: null })
        {
            throw new AuthenticationError();
        }

        var token = user.Tokens
            .OfType<EmailConfirmationToken>()
            .SingleOrDefault();

        if (token == null || token.Value != args.Token)
        {
            throw new AuthenticationError();
        }

        user.Tokens.Remove(token);
        user.EmailConfirmedOn = DateTime.UtcNow;

        await session.SaveChangesAsync();

        return null;
    }

    private async Task<object?> LoginAsync(IResolveFieldContext context)
    {
        // validate args

        var args = context.GetValidatedArgument<LoginArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var publicKey = Address.Parse(args.Address);

        var engiOptions = scope.ServiceProvider.GetRequiredService<IOptions<EngiOptions>>();

        if (!args.IsValid(publicKey, engiOptions.Value.SignatureSkew))
        {
            throw new AuthenticationError();
        }

        // find user

        using var session = scope.ServiceProvider
            .GetRequiredService<IAsyncDocumentSession>();

        var jwtOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<JwtOptions>>();

        string userId = User.KeyFrom(args.Address);

        var user = await session.LoadAsync<User>(userId);

        if (user?.EmailConfirmedOn == null)
        {
            throw new AuthenticationError();
        }

        var refreshToken = BuildRefreshToken(user, jwtOptions.Value);

        session.Advanced.Patch(user,
            x => x.Tokens,
            tokens => tokens.Add(refreshToken));

        await session.SaveChangesAsync();

        return new AuthenticationTokenPair
        {
            AccessToken = BuildAccessToken(user, jwtOptions.Value),
            RefreshToken = BuildRefreshToken(user, jwtOptions.Value)
        };
    }

    private async Task<object?> RefreshAsync(IResolveFieldContext context)
    {
        var enhancedContext = (EnhancedGraphQLContext)context.UserContext;

        var jwtOptions = context.RequestServices!
            .GetRequiredService<IOptions<JwtOptions>>();

        // verify refresh token from cookie

        string? refreshTokenValue = enhancedContext.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            throw new AuthenticationError();
        }

        // load user and check refresh token exists on this user

        string? userId = RefreshToken.DecryptUserId(refreshTokenValue, jwtOptions.Value.IssuerSigningKey);

        if (userId == null)
        {
            throw new AuthenticationError();
        }

        using var session = context.RequestServices!
            .GetRequiredService<IAsyncDocumentSession>();

        var user = await session
            .LoadAsync<User>(userId);

        if (user == null)
        {
            throw new AuthenticationError();
        }

        var refreshToken = user.Tokens
            .OfType<RefreshToken>()
            .FirstOrDefault(x => x.Value == refreshTokenValue);

        if (refreshToken == null)
        {
            throw new AuthenticationError();
        }

        // if expired, remove

        if (refreshToken.ExpiresOn! < DateTime.UtcNow)
        {
            session.Advanced.Patch(user,
                x => x.Tokens,
                tokens => tokens.RemoveAll(t => t.Id == refreshToken.Id));

            await session.SaveChangesAsync();

            throw new AuthenticationException();
        }

        // replace

        var newRefreshToken = BuildRefreshToken(user, jwtOptions.Value);

        session.Advanced.Patch(user,
            x => x.Tokens,
            tokens => tokens.RemoveAll(t => t.Id == refreshToken.Id));

        session.Advanced.Patch(user,
            x => x.Tokens,
            tokens => tokens.Add(newRefreshToken));

        await session.SaveChangesAsync();

        return new AuthenticationTokenPair
        {
            AccessToken = BuildAccessToken(user, jwtOptions.Value),
            RefreshToken = BuildRefreshToken(user, jwtOptions.Value)
        };
    }

    private async Task<object?> RegisterAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<CreateUserArguments>("user");

        // make sure we can decode the key

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var engiOptions = scope.ServiceProvider.GetRequiredService<IOptions<EngiOptions>>();
        var applicationOptions = scope.ServiceProvider.GetRequiredService<IOptions<ApplicationOptions>>();

        var privateKey = engiOptions.Value.EncryptionCertificateAsX509.GetRSAPrivateKey()!;

        Keypair keypair;

        try
        {
            var encryptedData = Convert.FromBase64String(args.EncryptedPkcs8Key);

            var keypairPkcs8 = privateKey.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);

            keypair = Keypair.FromPkcs8(keypairPkcs8);
        }
        catch (Exception)
        {
            throw new ExecutionError("Unable to decrypt and decode key.")
            {
                Code = "INVALID_KEY"
            };
        }

        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        // create user

        using var session = store.OpenAsyncSession(new SessionOptions
        {
            TransactionMode = TransactionMode.ClusterWide
        });

        var emailConfirmationToken = new EmailConfirmationToken();

        var user = new User
        {
            Id = User.KeyFrom(keypair.Address.Id),
            Email = args.Email.ToLowerInvariant().Trim(),
            Address = keypair.Address.Id,
            KeypairPkcs8 = keypair.ExportToPkcs8(engiOptions.Value.EncryptionCertificateAsX509),
            Tokens = { emailConfirmationToken }
        };

        await session.StoreAsync(user);

        var emailReference = new UserEmailReference(user);

        await session.StoreAsync(emailReference);

        await session.StoreAsync(new EmailDispatchCommand
        {
            UserId = user.Id,
            TemplateName = "ConfirmEmail",
            Data = new()
            {
                ["Url"] = $"{applicationOptions.Value.Url}/confirm/{user.Address}?token={emailConfirmationToken.Value}"
            }
        });

        try
        {
            await session.SaveChangesAsync();
        }
        catch (ConcurrencyException)
        {
            throw new ExecutionError("User already exists.")
            {
                Code = "DUPE_EMAIL"
            };
        }

        return user.Id;
    }

    private string BuildAccessToken(User user, JwtOptions jwtOptions)
    {
        var iat = DateTime.UtcNow;

        var claims = GetClaimsForUser(user, iat).ToArray();

        TimeSpan accessTokenValidFor = jwtOptions.AccessTokenValidFor;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = iat,
            Expires = iat + accessTokenValidFor,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(jwtOptions.IssuerSigningKey), "RS256")
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var jwt = tokenHandler.CreateJwtSecurityToken(descriptor);

        return tokenHandler.WriteToken(jwt);
    }

    private RefreshToken BuildRefreshToken(User user, JwtOptions jwtOptions)
    {
        return RefreshToken.Encrypt(user.Id, jwtOptions.IssuerSigningKey, jwtOptions.RefreshTokenValidFor);
    }

    private IEnumerable<Claim> GetClaimsForUser(User user, DateTimeOffset iat)
    {
        yield return new Claim("sub", user.Id);
        yield return new Claim("jti", Guid.NewGuid().ToString());
        yield return new Claim("iat", iat.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64);
        yield return new Claim(ClaimTypes.Name, user.Id);

        foreach (var role in user.SystemRoles)
        {
            yield return new Claim(ClaimTypes.Role, role);
        }
    }
}