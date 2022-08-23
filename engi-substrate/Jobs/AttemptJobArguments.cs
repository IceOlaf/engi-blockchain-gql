namespace Engi.Substrate.Jobs;

public class AttemptJobArguments : SignedExtrinsicArgumentsBase, IScaleSerializable
{
    public ulong JobId { get; init; }

    public string SubmissionPatchFileUrl { get; init; } = null!;
    
    public void Serialize(ScaleStreamWriter writer)
    {
        writer.Write(JobId);
        writer.Write(SubmissionPatchFileUrl);
    }
}