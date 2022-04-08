namespace Engi.Substrate.Metadata.V11;

public class Metadata
{
    public int MagicNumber { get; set; }

    public int Version { get; set; }

    public ModuleMetadata[]? Modules { get; set; }

    public ExtrinsicMetadata? Extrinsic { get; set; }
}