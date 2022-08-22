namespace Engi.Substrate.Metadata.V14;

public class StorageEntryMap : IStorageEntry
{
    public StorageHasher[] Hashers { get; set; } = null!;
    public TType Key { get; set; } = null!;
    public TType Value { get; set; } = null!;

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