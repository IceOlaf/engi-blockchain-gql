namespace Engi.Substrate.Metadata.V14;

public class StorageEntryMap : IStorageEntry
{
    public StorageHasher[]? Hashers { get; set; }
    public TType? Key { get; set; }
    public TType? Value { get; set; }

    public static StorageEntryMap Parse(ScaleStreamReader stream)
    {
        return new StorageEntryMap
        {
            Hashers = stream.ReadList(s => s.ReadEnum<StorageHasher>()),
            Key = TType.Parse(stream),
            Value = TType.Parse(stream)
        };
    }
}