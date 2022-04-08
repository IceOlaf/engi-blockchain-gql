namespace Engi.Substrate.Metadata.V11;

public class StorageEntryMap : IStorageEntry
{
    public StorageHasher Hasher { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
    public bool Linked { get; set; }
}