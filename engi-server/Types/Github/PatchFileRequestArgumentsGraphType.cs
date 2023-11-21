using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class PatchFileRequestArgumentsGraphType : InputObjectGraphType<PatchFileRequestArguments>
{
    public PatchFileRequestArgumentsGraphType()
    {
        Field(x => x.BaseRepositoryUrl)
            .Description("The base repository to compare against.");

        Field(x => x.BaseRepositoryCommit)
            .Description("Commit sha of base repository.");

        Field(x => x.ForkRepositoryUrl)
            .Description("The forked repository with the solution.");

        Field(x => x.ForkRepositoryCommit)
            .Description("Commit sha of the fork.");
    }
}
