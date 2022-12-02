using Engi.Substrate.Github;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Github;

public class GithubRepositoryOwnerGraphType : ObjectGraphType<GithubRepositoryOwner>
{
    public GithubRepositoryOwnerGraphType()
    {
        Field(x => x.Login)
            .Description("The repository owner's login.");

        Field(x => x.AvatarUrl, nullable: true)
            .Description("The repository owner's avatar URL.");
    }
}