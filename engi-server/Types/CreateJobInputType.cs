namespace Engi.Substrate.Server.Types;

public class CreateJobInputType : SignedExtrinsicInputTypeBase<CreateJobInput>
{
    public CreateJobInputType()
    {
        Field(x => x.Funding, type: typeof(BigIntegerType));
    }
}