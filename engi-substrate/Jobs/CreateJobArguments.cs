using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Jobs;

public class CreateJobArguments : SignedExtrinsicArgumentsBase, IScaleSerializable
{
    // TODO: create validation for U128
    [Range(500, ulong.MaxValue)]
    public ulong Funding { get; init; }

    public Language Language { get; init; }

    [Required, StringLength(100, MinimumLength = 12)]
    public string RepositoryUrl { get; init; } = null!;

    [Required, StringLength(20, MinimumLength = 1)]
    public string BranchName { get; init; } = null!;

    [Required, StringLength(64, MinimumLength = 6)]
    public string CommitHash { get; init; } = null!;

    [NotNullOrEmptyCollection, MaxLength(100)]
    public Test[] Tests { get; init; } = null!;

    [Required, StringLength(50, MinimumLength = 4)]
    public string Name { get; init; } = null!;

    [Required]
    public FilesRequirement FilesRequirement { get; init; } = null!;
    
    public void Serialize(ScaleStreamWriter writer)
    {
        writer.WriteCompact(Funding);
        writer.Write(Language);
        writer.Write(RepositoryUrl);
        writer.Write(BranchName);
        writer.Write(CommitHash);
        writer.Write(Tests);
        writer.Write(Name);
        writer.Write(FilesRequirement);
    }
}