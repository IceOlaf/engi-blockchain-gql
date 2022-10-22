using Engi.Substrate.Identity;
using Engi.Substrate.Server.Types.Github;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserGithubEnrollmentGraphType : ObjectGraphType<UserGithubEnrollment>
{
    public UserGithubEnrollmentGraphType()
    {
        // Note: doesn't expose repositories, they are queryable from the `github` query

        Field(x => x.InstallationId)
            .Description("The enrollment's installation id.");

        Field(x => x.CreatedOn)
            .Description("The date and time the user enrolled.");

        Field(x => x.Owner, type: typeof(GithubRepositoryOwnerGraphType))
            .Description("The Github user information for this enrollment.");
    }
}