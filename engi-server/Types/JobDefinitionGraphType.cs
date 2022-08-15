using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobDefinitionGraphType : ObjectGraphType<JobDefinition>
{
    public JobDefinitionGraphType()
    {
        Field(x => x.Funding, type: typeof(UInt128GraphType));
        Field(x => x.Language);
        Field(x => x.RepositoryUrl);
        Field(x => x.BranchName);
        Field(x => x.CommitHash);
        Field(x => x.Tests, type: typeof(JobTestGraphType));
        Field(x => x.Name);
        Field(x => x.FilesRequirement);
    }
}