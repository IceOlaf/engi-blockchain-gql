namespace Engi.Substrate.Metadata.V14;

public class RuntimeMetadata
{
    public int MagicNumber { get; set; }

    public int Version { get; set; }

    public Dictionary<ulong, PortableType> TypesById { get; set; } = null!;

    public PalletMetadataCollection Pallets { get; set; } = null!;

    public ExtrinsicMetadata Extrinsic { get; set; } = null!;

    public TType? TypeId { get; set; }

    public PalletMetadata FindPallet(string name)
    {
        return Pallets.Single(
            x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public (PalletMetadata pallet, Variant variant) FindPalletCallVariant(string palletName, string callName)
    {
        var pallet = FindPallet(palletName);

        var callType = TypesById[pallet.Calls.Type.Value];

        if (callType.Definition is VariantTypeDefinition variantType)
        {
            var variant = variantType.Variants.Single(
                x => string.Equals(x.Name, callName, StringComparison.OrdinalIgnoreCase));

            return (pallet, variant);
        }

        throw new InvalidOperationException($"Call definition is not a variant; type={callType}.");
    }

    public PortableType GetTypeByPath(params string[] path)
    {
        // TODO: cache

        return TypesById.Values
            .First(x => x.Path.SequenceEqual(path));
    }

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
            TypesById = stream.ReadList(PortableType.Parse)
                .ToDictionary(x => x.Id, x => x),
            Pallets = new PalletMetadataCollection(stream.ReadList(PalletMetadata.Parse)),
            Extrinsic = ExtrinsicMetadata.Parse(stream),
            TypeId = TType.Parse(stream)
        };
    }
}