namespace Engi.Substrate.Metadata.V14;

public class PalletErrorMetadata
{
    public TType? Type { get; set; }

    public static PalletErrorMetadata Parse(ScaleStream stream)
    {
        return new()
        {
            Type = TType.Parse(stream)
        };
    }
}