namespace Engi.Substrate.Metadata.V14;

public class StorageEntryPlain : IStorageEntry
{
    public TType? Value { get; set; }

    public static StorageEntryPlain Parse(ScaleStream stream)
    {
        return new()
        {
            Value = TType.Parse(stream)
        };
    }
}