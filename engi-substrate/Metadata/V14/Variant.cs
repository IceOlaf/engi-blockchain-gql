namespace Engi.Substrate.Metadata.V14;

public class Variant
{
    public string Name { get; set; } = null!;
    public FieldCollection Fields { get; set; } = null!;
    public byte Index { get; set; }
    public string?[]? Docs { get; set; }

    public override string ToString()
    {
        return Name;
    }

    public static Variant Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Name = stream.ReadString()!,
            Fields = new FieldCollection(stream.ReadList(Field.Parse)),
            Index = (byte)stream.ReadByte(),
            Docs = stream.ReadList(s => s.ReadString(false))
        };
    }
}