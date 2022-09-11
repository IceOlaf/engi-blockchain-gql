using Engi.Substrate.Server.Types.Authentication;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AllowAnonymousAttribute = Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute;

namespace Engi.Substrate.Server.Controllers;

[Route("api/graphql"), ApiController, AllowAnonymous]
public class GraphQLController : ControllerBase
{
    private readonly IDocumentExecuter documentExecuter;
    private readonly ISchema schema;
    private readonly IWebHostEnvironment environment;
    private readonly ApiOptions apiOptions;

    public GraphQLController(
        IDocumentExecuter documentExecuter, 
        ISchema schema,
        IWebHostEnvironment environment,
        IOptions<ApiOptions> apiOptions)
    {
        this.documentExecuter = documentExecuter;
        this.schema = schema;
        this.environment = environment;
        this.apiOptions = apiOptions.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Execute([FromBody] GraphQLRequest request)
    {
        var startTime = DateTime.UtcNow;

        void Configure(ExecutionOptions s)
        {
            s.Schema = schema;
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

        var result = await documentExecuter.ExecuteAsync(Configure);

        // hijack refresh tokens and return them as cookies

        if (result.Operation?.Operation == OperationType.Mutation
            && result.Data is ObjectExecutionNode { SubFields.Length: > 0 } rootNode 
            && rootNode.SubFields![0] is ObjectExecutionNode childNode 
            && childNode.FieldDefinition.ResolvedType is AuthMutations 
            && childNode.SubFields![0].Result is AuthenticationTokenPair tokenPair)
        {
            Response.Cookies.Append("refreshToken", tokenPair.RefreshToken.Value!, new()
            {
                Domain = apiOptions.Domain,
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                Expires = tokenPair.RefreshToken.ExpiresOn
            });
        }

        result.EnrichWithApolloTracing(startTime);

        return new ExecutionResultActionResult(result);
    }
}