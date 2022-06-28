namespace Engi.Substrate.Metadata.V14;

public class StorageEntryMetadata
{
    public string? Name { get; set; }
    public StorageEntryModifier Modifier { get; set; }
    public IStorageEntry? Type { get; set; }
    public byte[]? Default { get; set; }
    public string?[]? Docs { get; set; }

    public static StorageEntryMetadata Parse(ScaleStreamReader stream)
    {
        var item = new StorageEntryMetadata
        {
            Name = stream.ReadString(),
            Modifier = stream.ReadEnum<StorageEntryModifier>()
        };

        var type = stream.ReadEnum<StorageEntryType>();

        item.Type = type switch
        {
            StorageEntryType.Plain => StorageEntryPlain.Parse(stream),
            StorageEntryType.Map => StorageEntryMap.Parse(stream),
            _ => throw new NotImplementedException(type.ToString())
        };

        item.Default = stream.ReadByteArray();
        item.Docs = stream.ReadList(s => s.ReadString(false));

        return item;
    }
}