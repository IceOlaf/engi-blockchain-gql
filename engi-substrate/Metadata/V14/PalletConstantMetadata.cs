namespace Engi.Substrate.Metadata.V14;

public class PalletConstantMetadata
{
    public string? Name { get; set; }
    public TType Type { get; set; } = null!;
    public byte[]? Value { get; set; }
    public string?[]? Docs { get; set; }

    public static PalletConstantMetadata Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Name = stream.ReadString(),
            Type = TType.Parse(stream),
            Value = stream.ReadByteArray(),
            Docs = stream.ReadList(s => s.ReadString(false))
        };
    }
}