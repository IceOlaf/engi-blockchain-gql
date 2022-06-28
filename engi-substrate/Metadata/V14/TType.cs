namespace Engi.Substrate.Metadata.V14;

public class TType
{
    public ulong Value { get; set; }

    public static implicit operator ulong(TType type) => type.Value;

    public static TType Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Value = stream.ReadCompactInteger()
        };
    }
}