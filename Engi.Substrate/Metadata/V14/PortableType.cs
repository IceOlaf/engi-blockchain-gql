namespace Engi.Substrate.Metadata.V14;

public class PortableType
{
    public ulong Id { get; set; }

    public string[] Path { get; set; } = null!;

    public TypeParameter[] Params { get; set; } = null!;

    public TypeDefinition Definition { get; set; } = null!;

    public string[] Docs { get; set; } = null!;

    public override string ToString()
    {
        return string.Join(":", Path);
    }

    public static PortableType Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Id = stream.ReadCompactInteger(),
            Path = stream.ReadList(s => s.ReadString()!),
            Params = stream.ReadList(TypeParameter.Parse),
            Definition = TypeDefinition.Parse(stream)!,
            Docs = stream.ReadList(s => s.ReadString(false)!)
        };
    }
}