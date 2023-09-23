using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;
using GraphQL;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Types;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types.Engine;

public class AnalysisQuery : ObjectGraphType
{
    public AnalysisQuery()
    {
        Field<RepositoryAnalysisGraphType>("get")
            .Argument<NonNullGraphType<StringGraphType>>("id")
            .ResolveAsync(GetAnalysisAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);
    }

    private async Task<object?> GetAnalysisAsync(IResolveFieldContext<object?> context)
    {
        string id = context.GetArgument<string>("id");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        string currentUserId = context.User!.Identity!.Name!;

        var objects = await session
            .LoadAsync<object>(new[] { id, currentUserId });

        var analysis = (RepositoryAnalysis?)objects[id];
        var currentUser = (User)objects[currentUserId];

        if (analysis == null)
        {
            return null;
        }

        if (analysis.CreatedBy != currentUser.Address)
        {
            throw new AccessDeniedError(analysis.Id);
        }

        return analysis;
    }
}
