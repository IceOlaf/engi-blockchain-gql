using Engi.Substrate.Github;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Github;

public class GithubRepositoryWithOwnerGraphType : ObjectGraphType<GithubRepositoryWithOwner>
{
    public GithubRepositoryWithOwnerGraphType()
    {
        Field(x => x.Name)
            .Description("The repository name.");
        Field(x => x.FullName)
            .Description("The repository name, including the user or organization name.");
        Field(x => x.IsPrivate)
            .Description("A boolean indicating whether the repository is private.");
        Field(x => x.Owner, type: typeof(GithubRepositoryOwnerGraphType))
            .Description("Information about the repository owner.");
    }
}