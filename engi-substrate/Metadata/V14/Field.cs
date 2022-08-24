using System.Diagnostics;

namespace Engi.Substrate.Metadata.V14;

[DebuggerDisplay("{Name}")]
public class Field
{
    public string? Name { get; set; }

    public TType Type { get; set; } = null!;

    public string? TypeName { get; set; }

    public string[] Docs { get; set; } = null!;

    public static Field Parse(ScaleStreamReader stream)
    {
        return new Field
        {
            Name = stream.ReadOptional(s => s.ReadString()),
            Type = TType.Parse(stream),
            TypeName = stream.ReadOptional(s => s.ReadString()),
            Docs = stream.ReadList(s => s.ReadString(false)!)
        };
    }
}