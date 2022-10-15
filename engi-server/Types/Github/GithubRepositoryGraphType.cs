using Engi.Substrate.Github;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Github;

public class GithubRepositoryGraphType : ObjectGraphType<GithubRepository>
{
    public GithubRepositoryGraphType()
    {
        Field(x => x.Name)
            .Description("The repository name.");
        Field(x => x.FullName)
            .Description("The repository name, including the user or organization name.");
        Field(x => x.IsPrivate)
            .Description("A boolean indicating whether the repository is private.");
        Field(x => x.OwnerAvatarUrl)
            .Description("The avatar URL of the owner.");
    }
}