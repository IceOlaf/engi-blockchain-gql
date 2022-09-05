namespace Engi.Substrate.Metadata.V14;

public class PalletCallIndex : IScaleSerializable
{
    public byte PalletIndex { get; set; }

    public byte CallIndex { get; set; }

    public override string ToString()
    {
        return $"pallet index={PalletIndex}; call index={CallIndex}";
    }

    public void Serialize(ScaleStreamWriter writer)
    {
        writer.Write(PalletIndex);
        writer.Write(CallIndex);
    }

    public static PalletCallIndex Parse(ScaleStreamReader reader)
    {
        return new()
        {
            PalletIndex = (byte)reader.ReadByte(),
            CallIndex = (byte)reader.ReadByte()
        };
    }
}