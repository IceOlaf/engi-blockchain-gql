namespace Engi.Substrate.Metadata.V14;

public class StorageEntryPlain : IStorageEntry
{
    public TType Value { get; set; } = null!;

    public static StorageEntryPlain Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Value = TType.Parse(stream)
        };
    }
}