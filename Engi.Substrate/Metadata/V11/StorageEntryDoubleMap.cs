namespace Engi.Substrate.Metadata.V11;

public class StorageEntryDoubleMap : IStorageEntry
{
    public StorageHasher Hasher { get; set; }
    public string? Key1 { get; set; }
    public string? Key2 { get; set; }
    public string? Value { get; set; }
    public StorageHasher Key2Hasher { get; set; }
}