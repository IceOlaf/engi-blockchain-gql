using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Github;

public class GithubEnrollmentArgumentsGraphType : InputObjectGraphType<GithubEnrollmentArguments>
{
    public GithubEnrollmentArgumentsGraphType()
    {
        Field(x => x.Code)
            .Description("The code returned from GitHub after app installation.");

        Field(x => x.InstallationId)
            .Description("The app installation id.");
    }
}