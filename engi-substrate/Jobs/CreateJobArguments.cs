using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public class CreateJobArguments : IExtrinsic
{
    public string PalletName => ChainKeys.Jobs.Name;

    public string CallName => ChainKeys.Jobs.Calls.CreateJob;

    // TODO: create validation for U128
    [Range(500, ulong.MaxValue)]
    public ulong Funding { get; init; }

    [NotNullOrEmptyCollection]
    public Technology[] Technologies { get; init; } = Array.Empty<Technology>();

    [Required, HttpUrl]
    public string RepositoryUrl { get; init; } = null!;

    [Required, StringLength(20, MinimumLength = 1)]
    public string BranchName { get; init; } = null!;

    [Required, StringLength(64, MinimumLength = 6)]
    public string CommitHash { get; init; } = null!;

    [NotNullOrEmptyCollection, MaxLength(100)]
    public Test[] Tests { get; init; } = null!;

    [Required, StringLength(50, MinimumLength = 4)]
    public string Name { get; init; } = null!;

    public FilesRequirement? FilesRequirement { get; init; }

    public IEnumerable<Func<Field, PortableType, PortableType?, bool>> GetVariantAssertions()
    {
        return new Func<Field, PortableType, PortableType?, bool>[]
        {
            (field, type, innerType) => field.Name == "funding" && type.Definition is CompactTypeDefinition && innerType!.FullName == "u128",
            (field, type, _) => field.Name == "language" && type.Definition is VariantTypeDefinition,
            (field, _, _) => field.Name == "repository_url",
            (field, _, _) => field.Name == "branch_name",
            (field, _, _) => field.Name == "commit_hash",
            (field, _, innerType) => field.Name == "tests" && innerType!.Definition is CompositeTypeDefinition { Fields.Count: 3 },
            (field, _, _) => field.Name == "name",
            (field, type, _) => field.Name == "files_requirement" && type is { FullName: "Option", Params.Length: 1 }
                                                                  && type.Params[0].Type!.Reference!.Definition is TupleTypeDefinition { Fields.Length: 3 }
        };
    }

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata meta)
    {
        writer.WriteCompact(Funding);
        writer.Write(Technologies);
        writer.Write(RepositoryUrl);
        writer.Write(BranchName);
        writer.Write(CommitHash);
        writer.Write(Tests, meta);
        writer.Write(Name);
        writer.WriteOptional(FilesRequirement != null,
            writer => writer.Write(FilesRequirement!, meta));
    }
}
