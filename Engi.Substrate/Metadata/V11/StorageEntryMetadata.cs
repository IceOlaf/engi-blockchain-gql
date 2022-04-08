namespace Engi.Substrate.Metadata.V11;

public class StorageEntryMetadata
{
    public string? Name { get; set; }
    public StorageEntryModifier Modifier { get; set; }
    public StorageEntryType TyType { get; set; }
    public IStorageEntry? Type { get; set; }
    public byte[]? Fallback { get; set; }
    public string[]? Docs { get; set; }
}