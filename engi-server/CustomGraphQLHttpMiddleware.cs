using System.Net;
using System.Text.Json;
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

        userContext["cookies"] = context.Request.Cookies;

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
                ["Query"] = request.Query ?? string.Empty,
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
                && childNode.FieldDefinition.ResolvedType is AuthMutations
                && childNode.SubFields![0].Result is AuthenticationTokenPair tokenPair)
            {
                context.Response.Cookies.Append("refreshToken", tokenPair.RefreshToken.Value!, new()
                {
                    Domain = apiOptions.ApiDomain,
                    HttpOnly = true,
                    Secure = !environment.IsDevelopment(),
                    Expires = tokenPair.RefreshToken.ExpiresOn
                });
            }
        }

        await WriteJsonResponseAsync(context, statusCode, result);
    }
}
