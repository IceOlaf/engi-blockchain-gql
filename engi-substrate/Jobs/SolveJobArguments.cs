using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public class SolveJobArguments : IExtrinsic
{
    public string PalletName => ChainKeys.Jobs.Name;

    public string CallName => ChainKeys.Jobs.Calls.SolveJob;

    public ulong SolutionId { get; set; }

    public ulong JobId { get; set; }

    [Required]
    public Address Author { get; set; } = null!;

    [Required, HttpUrl]
    public string PatchFileUrl { get; set; } = null!;

    [Required]
    public Attempt Attempt { get; set; } = null!;

    public IEnumerable<Func<Field, PortableType, PortableType?, bool>> GetVariantAssertions()
    {
        return new Func<Field, PortableType, PortableType?, bool>[]
        {
            (field, fieldType, _) => field.Name == "solution" 
                    && fieldType.FullName == "pallet_jobs:pallet:Solution"
                    && fieldType.Definition is CompositeTypeDefinition composite 
                    && composite.Fields.Count == 5
        };
    }

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata meta)
    {
        writer.Write(SolutionId);
        writer.Write(JobId);
        writer.Write(Author, meta);
        writer.Write(PatchFileUrl);
        writer.Write(Attempt, meta);
    }
}
