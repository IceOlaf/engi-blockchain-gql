namespace Engi.Substrate.Metadata.V14;

public class PortableType
{
    public ulong Id { get; set; }

    public TypePortableForm? Type { get; set; }

    public static PortableType Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Id = stream.ReadCompactInteger(),
            Type = TypePortableForm.Parse(stream)
        };
    }
}