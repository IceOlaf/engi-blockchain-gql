namespace Engi.Substrate.Metadata.V14;

public class PalletErrorMetadata
{
    public TType Type { get; set; } = null!;

    public static PalletErrorMetadata Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Type = TType.Parse(stream)
        };
    }
}