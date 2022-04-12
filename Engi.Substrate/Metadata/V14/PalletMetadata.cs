namespace Engi.Substrate.Metadata.V14;

public class PalletMetadata
{
    public string? Name { get; set; }

    public PalletStorageMetadata? Storage { get; set; }

    public PalletCallMetadata? Calls { get; set; }

    public PalletEventMetadata? Events { get; set; }

    public PalletConstantMetadata[]? Constants { get; set; }

    public PalletErrorMetadata? Errors { get; set; }

    public int Index { get; set; }

    public static PalletMetadata Parse(ScaleStream stream)
    {
        var metadata = new PalletMetadata();
        metadata.Name = stream.ReadString();
        metadata.Storage = stream.ReadOptional(PalletStorageMetadata.Parse);
        metadata.Calls = stream.ReadOptional(PalletCallMetadata.Parse);
        metadata.Events = stream.ReadOptional(PalletEventMetadata.Parse);
        metadata.Constants = stream.ReadList(PalletConstantMetadata.Parse);
        metadata.Errors = stream.ReadOptional(PalletErrorMetadata.Parse);
        metadata.Index = stream.ReadByte();
        return metadata;
    }
}