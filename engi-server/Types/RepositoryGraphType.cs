using Engi.Substrate.Github;
using Engi.Substrate.Jobs;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types;

public class RepositoryGraphType : ObjectGraphType<Repository>
{
    public RepositoryGraphType()
    {
        Description = "Repository information for an ENGI job.";

        Field(x => x.Url)
            .Description("The repository url.");
        Field(x => x.Branch)
            .Description("The repository branch.");
        Field(x => x.Commit)
            .Description("The commit hash.");
        Field(x => x.Organization)
            .Description("The repository organization. e.g. for https://github.com/engi-network/blockchain, 'engi-network'.");
        Field(x => x.Name)
            .Description("The repository name. e.g. for https://github.com/engi-network/blockchain, 'blockchain'.");
        Field(x => x.FullName)
            .Description("The full name of the repository slug. e.g. for https://github.com/engi-network/blockchain, 'engi-network/blockchain'.");
        Field<StringGraphType>("readme")
            .Description("The repository's README or null if it has not been retrieved yet, or is not available.")
            .ResolveAsync(LoadReadmeAsync);
    }

    private async Task<object?> LoadReadmeAsync(IResolveFieldContext<Repository> context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        string readmeId = GithubRepositoryReadme.KeyFrom(context.Source.FullName);

        var readme = await session.LoadAsync<GithubRepositoryReadme>(readmeId);

        return readme?.Content;
    }
}