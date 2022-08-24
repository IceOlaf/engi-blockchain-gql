using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class AttemptJobArgumentsGraphType : SignedExtrinsicArgumentsGraphTypeBase<AttemptJobArguments>
{
    public AttemptJobArgumentsGraphType()
    {
        Description = "Arguments for the attempt_job signed extrinsic.";

        Field(x => x.JobId)
            .Description("The job id.");
        Field(x => x.SubmissionPatchFileUrl)
            .Description("A URL containing the submitted patch file.");
    }
}