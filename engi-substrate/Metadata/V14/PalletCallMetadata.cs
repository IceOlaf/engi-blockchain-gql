namespace Engi.Substrate.Metadata.V14;

public class PalletCallMetadata
{
    public TType Type { get; set; } = null!;

    public static PalletCallMetadata Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Type = TType.Parse(stream)
        };
    }
}