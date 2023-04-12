using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class CreateJobArgumentsGraphType : InputObjectGraphType<CreateJobArguments>
{
    public CreateJobArgumentsGraphType()
    {
        Description = "Arguments for the create_job signed extrinsic.";

        Field(x => x.Funding, type: typeof(UInt128GraphType))
            .Description("The funding for the job.");
        Field(x => x.Technologies)
            .Description("The main repository technologies.");
        Field(x => x.RepositoryUrl)
            .Description("The repository URL.");
        Field(x => x.BranchName)
            .Description("The relevant branch name.");
        Field(x => x.CommitHash)
            .Description("The relevant commit hash.");
        Field(x => x.Tests, type: typeof(ListGraphType<JobTestInputGraphType>))
            .Description("The tests that participate in this job.");
        Field(x => x.Name)
            .Description("The job name.");
        Field(x => x.FilesRequirement, type: typeof(FilesRequirementArgumentsGraphType), nullable: true)
            .Description("Regex or glob patterns files that define files as requirements for this job.");
    }
}
