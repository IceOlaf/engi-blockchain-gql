namespace Engi.Substrate.Metadata.V14;

public class TypeParameter
{
    public string? Name { get; set; }

    public TType? Type { get; set; }

    public static TypeParameter Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Name = stream.ReadString(),
            Type = stream.ReadOptional(TType.Parse)
        };
    }
}