using System.Text.Json;
using Engi.Substrate.Server.Types.Authentication;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sentry;
using AllowAnonymousAttribute = Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace Engi.Substrate.Server.Controllers;

[Route("api/graphql"), ApiController, AllowAnonymous]
public class GraphQLController : ControllerBase
{
    private readonly IDocumentExecuter documentExecuter;
    private readonly IWebHostEnvironment environment;
    private readonly IHub sentry;
    private readonly ILogger logger;
    private readonly ApplicationOptions apiOptions;

    public GraphQLController(
        IDocumentExecuter documentExecuter, 
        IWebHostEnvironment environment,
        IHub sentry,
        ILogger<GraphQLController> logger,
        IOptions<ApplicationOptions> apiOptions)
    {
        this.documentExecuter = documentExecuter;
        this.environment = environment;
        this.sentry = sentry;
        this.logger = logger;
        this.apiOptions = apiOptions.Value;
    }

    [HttpPost, Authorize(AuthenticationSchemes = AuthenticationSchemes.ApiKey)]
    public async Task<IActionResult> Execute([FromBody] GraphQLRequest request)
    {
        sentry.AddBreadcrumb("Processing GraphQL request",
            data: new Dictionary<string, string>
            {
                ["OperationName"] = request.OperationName ?? string.Empty,
                ["Query"] = request.Query ?? string.Empty,
                ["Variables"] = JsonSerializer.Serialize(request.Variables),
                ["Extensions"] = JsonSerializer.Serialize(request.Extensions)
            });

        var startTime = DateTime.UtcNow;

        var result = await documentExecuter
            .ExecuteAsync(options => Configure<RootSchema>(request, options));

        // hijack refresh tokens and return them as cookies

        if (result.Operation?.Operation == OperationType.Mutation
            && result.Data is ObjectExecutionNode { SubFields.Length: > 0 } rootNode 
            && rootNode.SubFields![0] is ObjectExecutionNode childNode 
            && childNode.FieldDefinition.ResolvedType is AuthMutations 
            && childNode.SubFields![0].Result is AuthenticationTokenPair tokenPair)
        {
            Response.Cookies.Append("refreshToken", tokenPair.RefreshToken.Value!, new()
            {
                Domain = apiOptions.ApiDomain,
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                Expires = tokenPair.RefreshToken.ExpiresOn
            });
        }

        result.EnrichWithApolloTracing(startTime);

        if (result.Errors?.OfType<AuthenticationError>().Any() == true)
        {
            return Unauthorized();
        }

        if (result.Errors?.OfType<AccessDeniedError>().Any() == true)
        {
            return Unauthorized();
        }

        return new ExecutionResultActionResult(result);
    }

    private void Configure<TSchema>(GraphQLRequest request, ExecutionOptions s)
        where TSchema : ISchema, new()
    {
        s.Schema = new TSchema();
        s.Query = request.Query;
        s.Variables = request.Variables;
        s.OperationName = request.OperationName;
        s.RequestServices = HttpContext.RequestServices;
        s.User = User;
        s.UserContext = new EnhancedGraphQLContext
        {
            Cookies = Request.Cookies
        };
        s.CancellationToken = HttpContext.RequestAborted;
    }
}
