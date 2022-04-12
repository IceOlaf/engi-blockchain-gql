namespace Engi.Substrate.Metadata.V14;

public class PalletEventMetadata
{
    public TType Type { get; set; }

    public static PalletEventMetadata Parse(ScaleStream stream)
    {
        return new()
        {
            Type = TType.Parse(stream)
        };
    }
}