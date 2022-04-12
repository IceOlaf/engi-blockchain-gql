namespace Engi.Substrate.Metadata.V14;

public class Field
{
    public string? Name { get; set; }

    public TType? Type { get; set; }

    public string? TypeName { get; set; }

    public string?[]? Docs { get; set; }

    public static Field Parse(ScaleStream stream)
    {
        return new Field
        {
            Name = stream.ReadOptional(s => s.ReadString()),
            Type = TType.Parse(stream),
            TypeName = stream.ReadOptional(s => s.ReadString()),
            Docs = stream.ReadList(s => s.ReadString(false))
        };
    }
}