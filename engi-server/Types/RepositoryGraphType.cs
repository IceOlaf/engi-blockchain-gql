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
        Field(x => x.Organization)
            .Description("The repository organization. e.g. for https://github.com/engi-network/blockchain, 'engi-network'.");
        Field(x => x.Name)
            .Description("The repository name. e.g. for https://github.com/engi-network/blockchain, 'blockchain'.");
        Field(x => x.FullName)
            .Description("The full name of the repository slug. e.g. for https://github.com/engi-network/blockchain, 'engi-network/blockchain'.");
    }
}