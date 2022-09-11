using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public class AttemptJobArguments : IExtrinsic
{
    public string PalletName => ChainKeys.Jobs.Name;

    public string CallName => ChainKeys.Jobs.Calls.AttemptJob;

    public ulong JobId { get; init; }

    [Required, MaxLength(100)]
    public string SubmissionPatchFileUrl { get; init; } = null!;

    public IEnumerable<Func<Field, PortableType, PortableType?, bool>> GetVariantAssertions()
    {
        return new Func<Field, PortableType, PortableType?, bool>[]
        {
            (field, type, _) => field.Name == "job_id" && type.FullName == "u64",
            (field, _, _) => field.Name == "submission_patch_file_url"
        };
    }

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata _)
    {
        writer.Write(JobId);
        writer.Write(SubmissionPatchFileUrl);
    }
}