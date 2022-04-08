namespace Engi.Substrate.Metadata.V11;

public class StorageEntryNMap : IStorageEntry
{
    public string[]? Keys { get; set; }
    public StorageHasher[]? Hashers { get; set; }
    public string? Value { get; set; }
}