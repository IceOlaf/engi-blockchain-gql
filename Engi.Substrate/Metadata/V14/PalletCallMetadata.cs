namespace Engi.Substrate.Metadata.V14;

public class PalletCallMetadata
{
    public TType? Type { get; set; }
    public static PalletCallMetadata Parse(ScaleStream stream)
    {
        return new()
        {
            Type = TType.Parse(stream)
        };
    }
}