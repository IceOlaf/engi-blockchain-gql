using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Engi.Substrate.Server.Types.Authentication;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Transport;
using GraphQLParser.AST;
using Microsoft.Extensions.Options;
using Sentry;

namespace Engi.Substrate.Server;

public class CustomGraphQLHttpMiddleware : GraphQLHttpMiddleware<RootSchema>
{
    private const string SessionKey = "User.Session";
    private readonly GraphQLHttpMiddlewareOptions options;
    private readonly ApplicationOptions apiOptions;
    private readonly IHub sentry;
    private readonly IWebHostEnvironment environment;

    public CustomGraphQLHttpMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<RootSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options,
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<ApplicationOptions> apiOptions,
        IHub sentry,
        IWebHostEnvironment environment)
        : base(next, serializer, documentExecuter, serviceScopeFactory, options, hostApplicationLifetime)
    {
        this.options = options;
        this.apiOptions = apiOptions.Value;
        this.sentry = sentry;
        this.environment = environment;
    }

    protected override async ValueTask<IDictionary<string, object?>?> BuildUserContextAsync(HttpContext context, object? payload)
    {
        var userContext = await base.BuildUserContextAsync(context, payload)
            ?? new Dictionary<string, object?>();

        var userData = context.Session.GetString(SessionKey);
        if (userData != null)
        {
            var sessionInfo = JsonSerializer.Deserialize<SessionInfo>(userData)!;

            var principal = new ClaimsPrincipal();
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(ClaimTypes.Name, sessionInfo.UserId));
            identity.AddClaim(new Claim("role", sessionInfo.Role));
            principal.AddIdentity(identity);

            userContext["session"] = sessionInfo;
            context.User = principal;
        }

        return userContext;
    }

    protected override async Task HandleRequestAsync(
        HttpContext context,
        RequestDelegate next,
        GraphQLRequest request)
    {
        var userContext = await BuildUserContextAsync(context, null);

        sentry.AddBreadcrumb("Processing GraphQL request",
            data: new Dictionary<string, string>
            {
                ["OperationName"] = request.OperationName ?? string.Empty,
                ["Query"] = SanitizeQuery(request.Query),
                ["Variables"] = JsonSerializer.Serialize(request.Variables),
                ["Extensions"] = JsonSerializer.Serialize(request.Extensions)
            });


        var result = await ExecuteRequestAsync(context, request, context.RequestServices, userContext);

        HttpStatusCode statusCode = HttpStatusCode.OK;

        if (!result.Executed)
        {
            if (result.Errors?.Any(e => e is HttpMethodValidationError) == true)
            {
                statusCode = HttpStatusCode.MethodNotAllowed;
            }
            else if (options.ValidationErrorsReturnBadRequest)
            {
                statusCode = HttpStatusCode.BadRequest;
            }
        }
        else
        {
            if (result.Errors?.OfType<AuthenticationError>().Any() == true)
            {
                statusCode = HttpStatusCode.Unauthorized;
            }
            else if (result.Errors?.OfType<AccessDeniedError>().Any() == true)
            {
                statusCode = HttpStatusCode.Forbidden;
            }
            // hijack refresh tokens and return them as cookies
            else if (result.Operation?.Operation == OperationType.Mutation
                && result.Data is ObjectExecutionNode { SubFields.Length: > 0 } rootNode
                && rootNode.SubFields![0] is ObjectExecutionNode childNode
                && childNode.FieldDefinition.ResolvedType is AuthMutations)
            {
                if (childNode.SubFields![0].Result is LoginResult loginResult)
                {
                    context.Session.SetString(SessionKey, loginResult.SessionToken);
                }
                else if (childNode.SubFields![0].Result is LogoutResult)
                {
                    context.Session.Remove(SessionKey);
                }
            }
        }

        await WriteJsonResponseAsync(context, statusCode, result);
    }

    private static string SanitizeQuery(string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return string.Empty;
        }

        return RemoveSignatureRegex.Replace(query, "signature: <redacted>");
    }

    private static readonly Regex RemoveSignatureRegex =
        new(@"signature:\s*{(.+?)}", RegexOptions.Compiled | RegexOptions.Singleline);
}
