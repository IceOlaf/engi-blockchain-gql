namespace Engi.Substrate.Metadata.V14;

public class RuntimeMetadata
{
    public int MagicNumber { get; set; }

    public int Version { get; set; }

    public PortableType[]? Types { get; set; }

    public PalletMetadata[]? Pallets { get; set; }

    public ExtrinsicMetadata? Extrinsic { get; set; }

    public TType? TypeId { get; set; }

    public static RuntimeMetadata Parse(ScaleStreamReader stream)
    {
        const int metadataMagicNumber = 0x6174656D;

        int magicNumber = stream.ReadInt32();

        if (magicNumber != metadataMagicNumber)
        {
            throw new InvalidDataException(
                $"Expected magic number 0x{metadataMagicNumber:x} but found 0x{magicNumber:x}");
        }

        int version = stream.ReadByte();

        if (version != 14)
        {
            throw new InvalidDataException("Only know how to parse v14.");
        }

        return new()
        {
            MagicNumber = magicNumber,
            Version = version,
            Types = stream.ReadList(PortableType.Parse),
            Pallets = stream.ReadList(PalletMetadata.Parse),
            Extrinsic = ExtrinsicMetadata.Parse(stream),
            TypeId = TType.Parse(stream)
        };
    }
}