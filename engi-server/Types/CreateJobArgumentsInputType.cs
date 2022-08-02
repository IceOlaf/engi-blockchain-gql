namespace Engi.Substrate.Server.Types;

public class CreateJobArgumentsInputType : SignedExtrinsicArgumentsGraphTypeBase<CreateJobArguments>
{
    public CreateJobArgumentsInputType()
    {
        Field(x => x.Job, type: typeof(JobDefinitionInputGraphType))
            .Description("The job definition.");
    }
}