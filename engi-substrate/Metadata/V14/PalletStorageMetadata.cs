namespace Engi.Substrate.Metadata.V14;

public class PalletStorageMetadata
{
    public string? Prefix { get; set; }
    public StorageEntryMetadata[] Items { get; set; } = null!;

    public static PalletStorageMetadata Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Prefix = stream.ReadString(),
            Items = stream.ReadList(StorageEntryMetadata.Parse)
        };
    }
}