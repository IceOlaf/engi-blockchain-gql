using Engi.Substrate.Jobs;
using GraphQL.Types;

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
    }
}