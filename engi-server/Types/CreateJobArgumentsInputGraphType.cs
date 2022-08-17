namespace Engi.Substrate.Server.Types;

public class CreateJobArgumentsInputGraphType : SignedExtrinsicArgumentsGraphTypeBase<CreateJobArguments>
{
    public CreateJobArgumentsInputGraphType()
    {
        Description = "The arguments for the create_job signed extrinsic.";

        Field(x => x.Funding, type: typeof(UInt128GraphType))
            .Description("The funding for the job.");
        Field(x => x.Language)
            .Description("The main repository language.");
        Field(x => x.RepositoryUrl)
            .Description("The repository URL.");
        Field(x => x.BranchName)
            .Description("The relevant branch name.");
        Field(x => x.CommitHash)
            .Description("The relevant commit hash.");
        Field(x => x.Tests, type: typeof(JobTestInputGraphType))
            .Description("The tests that participate in this job.");
        Field(x => x.Name)
            .Description("The job name.");
        Field(x => x.FilesRequirement)
            .Description("Any files that are a requirement for this job.");
    }
}