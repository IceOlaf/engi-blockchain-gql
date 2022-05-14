namespace Engi.Substrate.Metadata.V14;

public class Variant
{
    public string? Name { get; set; }
    public Field[]? Fields { get; set; }
    public int Index { get; set; }
    public string?[]? Docs { get; set; }

    public static Variant Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Name = stream.ReadString(),
            Fields = stream.ReadList(Field.Parse),
            Index = stream.ReadByte(),
            Docs = stream.ReadList(s => s.ReadString(false))
        };
    }
}