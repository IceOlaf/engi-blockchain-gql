using System.Numerics;

namespace Engi.Substrate.Jobs;

public class CreateJobArguments : SignedExtrinsicArgumentsBase, IScaleSerializable
{
    public BigInteger Funding { get; init; }

    public Language Language { get; init; }

    public string RepositoryUrl { get; init; } = null!;

    public string BranchName { get; init; } = null!;

    public string CommitHash { get; init; } = null!;

    public Test[] Tests { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string[] FilesRequirement { get; init; } = null!;
    
    public void Serialize(ScaleStreamWriter writer)
    {
        writer.WriteCompact(Funding);
        writer.Write(Language);
        writer.Write(RepositoryUrl);
        writer.Write(BranchName);
        writer.Write(CommitHash);
        writer.Write(Tests);
        writer.Write(Name);
        
        // this is a tuple so can't use the array overload directly
        foreach (var requirement in FilesRequirement)
        {
            writer.Write(requirement);
        }
    }
}