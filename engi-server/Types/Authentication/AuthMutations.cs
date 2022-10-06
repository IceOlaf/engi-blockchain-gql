using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
            .Description(@"
                Login mutation to get access and refresh token. The access token is returned in 
                the mutation response, while the refresh token is stored in a secure cookie.
                If the user is not found, hasn't confirmed their email or the submitted signature
                cannot be validated, code AUTHENTICATION_FAILED is returned as error.
            ")
            .Argument<NonNullGraphType<LoginArgumentsGraphType>>("args")
            .ResolveAsync(LoginAsync);

        Field<IdGraphType>("refresh")
            .Description(@"
                This mutation will return a new access token for the user, if the transmitted
                refresh token (secure cookie) is valid and hasn't expired. AUTHENTICATION_FAILED is
                returned otherwise.
            ")
            .ResolveAsync(RefreshAsync);

        Field<IdGraphType>("register")
            .Description(@"
                Create a new account on ENGI. A signature must be produced, to verify that the address,
                submitted is owned by the user. If signature validation fails, code AUTHENTICATION_FAILED
                is returned.
                If the e-mail submitted already exists on the system, code DUPE_EMAIL is returned. 
                Similarly, if the address submitted already exists, code DUPE_ADDRESS is returned. 
            ")
            .Argument<NonNullGraphType<CreateUserArgumentsGraphType>>("user")
            .Argument<NonNullGraphType<SignatureArgumentsGraphType>>("signature")
            .ResolveAsync(RegisterAsync);

        Field<IdGraphType>("confirmEmail")
            .Description(@"
                Confirm a user's account with the token sent over e-mail. If the address and token match,
                the user is activated. Otherwise, code AUTHENTICATION_FAILED is returned.
            ")
            .Argument<NonNullGraphType<ConfirmEmailArgumentsGraphType>>("args")
            .ResolveAsync(ConfirmEmailAsync);
    }

    private async Task<object?> ConfirmEmailAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<ConfirmEmailArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider
            .GetRequiredService<IAsyncDocumentSession>();

        var userAddressRef = await session
            .LoadAsync<UserAddressReference>(UserAddressReference.KeyFrom(args.Address),
                include => include.IncludeDocuments(x => x.UserId));

        if (userAddressRef == null)
        {
            throw new AuthenticationError();
        }

        var user = session.LoadAsync<User>(userAddressRef.UserId).Result;

        if (user.EmailConfirmedOn.HasValue)
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

        var crypto = scope.ServiceProvider.GetRequiredService<UserCryptographyService>();

        // find user

        using var session = scope.ServiceProvider
            .GetRequiredService<IAsyncDocumentSession>();

        var jwtOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<JwtOptions>>();

        var userAddressRef = await session
            .LoadAsync<UserAddressReference>(UserAddressReference.KeyFrom(args.Address),
                include => include.IncludeDocuments(x => x.UserId));

        if (userAddressRef == null)
        {
            throw new AuthenticationError();
        }

        var user = session.LoadAsync<User>(userAddressRef.UserId).Result;

        if (user.EmailConfirmedOn == null)
        {
            throw new AuthenticationError();
        }

        crypto.ValidateOrThrow(user, args.Signature);

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

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var engiOptions = scope.ServiceProvider.GetRequiredService<IOptions<EngiOptions>>();
        var applicationOptions = scope.ServiceProvider.GetRequiredService<IOptions<ApplicationOptions>>();

        // make sure we can decode the key, if there is one

        Keypair? keypair = null;

        if (args.EncryptedPkcs8Key != null)
        {
            var privateKey = engiOptions.Value.EncryptionCertificateAsX509.GetRSAPrivateKey()!;

            try
            {
                var encryptedData = Convert.FromBase64String(args.EncryptedPkcs8Key);

                var decrypted = privateKey.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);

                var keypairPkcs8 = Convert.FromBase64String(Encoding.UTF8.GetString(decrypted));

                keypair = Keypair.FromPkcs8(keypairPkcs8);
            }
            catch (Exception)
            {
                throw new ExecutionError("Unable to decrypt and decode key.")
                {
                    Code = "INVALID_KEY"
                };
            }
        }

        // create user

        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        using var session = store.OpenAsyncSession(new SessionOptions
        {
            TransactionMode = TransactionMode.ClusterWide
        });

        var emailConfirmationToken = new EmailConfirmationToken();

        var user = new User
        {
            Email = args.Email.ToLowerInvariant().Trim(),
            Display = args.Display,
            Tokens = { emailConfirmationToken }
        };

        if (keypair != null)
        {
            user.Address = keypair.Address.Id;
            user.KeypairPkcs8 = keypair.ExportToPkcs8(engiOptions.Value.EncryptionCertificateAsX509);
        }

        await session.StoreAsync(user);

        await session.StoreAsync(new UserEmailReference(user));
        await session.StoreAsync(new UserAddressReference(user));

        await session.StoreAsync(new EmailDispatchCommand
        {
            UserId = user.Id,
            TemplateName = "ConfirmEmail",
            Data = new()
            {
                ["Url"] = $"{applicationOptions.Value.Url}/signup/confirm/{user.Address}?token={emailConfirmationToken.Value}"
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