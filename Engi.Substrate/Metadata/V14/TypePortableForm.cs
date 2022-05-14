namespace Engi.Substrate.Metadata.V14;

public class TypePortableForm
{
    public string?[]? Path { get; set; }

    public TypeParameter[]? Params { get; set; }

    public TypeDefinition? Definition { get; set; }

    public string?[]? Docs { get; set; }

    public static TypePortableForm Parse(ScaleStreamReader stream)
    {
        return new TypePortableForm
        {
            Path = stream.ReadList(s => s.ReadString()),
            Params = stream.ReadList(TypeParameter.Parse),
            Definition = TypeDefinition.Parse(stream),
            Docs = stream.ReadList(s => s.ReadString(false))
        };
    }
}