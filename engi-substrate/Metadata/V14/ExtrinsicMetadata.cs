namespace Engi.Substrate.Metadata.V14;

public class ExtrinsicMetadata
{
    public TType Type { get; set; } = null!;
    public int Version { get; set; }
    public SignedExtensionMetadata[]? SignedExtensions { get; set; }
    
    public static ExtrinsicMetadata Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Type = TType.Parse(stream),
            Version = stream.ReadByte(),
            SignedExtensions = stream.ReadList(SignedExtensionMetadata.Parse)
        };
    }
}