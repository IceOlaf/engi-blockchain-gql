namespace Engi.Substrate.Metadata.V14;

public class SignedExtensionMetadata
{
    public string? Identifier { get; set; }
    public TType? Type { get; set; }
    public TType? AdditionalSigned { get; set; }

    public static SignedExtensionMetadata Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Identifier = stream.ReadString(),
            Type = TType.Parse(stream),
            AdditionalSigned = TType.Parse(stream)
        };
    }
}