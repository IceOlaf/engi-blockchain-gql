using System.Linq.Expressions;

namespace Engi.Substrate.Metadata.V14;

public class RuntimeMetadata
{
    public int MagicNumber { get; set; }

    public int Version { get; set; }

    public Dictionary<ulong, PortableType> TypesById { get; set; } = null!;

    public PalletMetadataCollection Pallets { get; set; } = null!;

    public ExtrinsicMetadata Extrinsic { get; set; } = null!;

    public TType? TypeId { get; set; }

    public VariantTypeDefinition MultiAddressTypeDefinition
    {
        get
        {
            // TODO: cache

            var type = GetTypeByFullName("sp_runtime:multiaddress:MultiAddress");

            return (VariantTypeDefinition)type.Definition;
        }
    }

    public PalletMetadata FindPallet(int index)
    {
        try
        {
            return Pallets.Single(x => x.Index == index);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Pallet with index '{index}' was not found.");
        }
    }

    public PalletMetadata FindPallet(string name)
    {
        try
        {
            return Pallets.Single(
                x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        catch(InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Pallet '{name}' was not found.");
        }
    }

    public (PalletMetadata pallet, Variant @event) FindEvent(byte moduleIndex, byte eventIndex)
    {
        var pallet = FindPallet(moduleIndex);

        if (pallet.Events?.Type == null)
        {
            throw new InvalidOperationException(
                $"Event with index '{eventIndex}' was not found in pallet '{pallet.Name}' (index={moduleIndex}).");
        }

        var eventsTypeIndex = pallet.Events!.Type!.Value;
        var events = (VariantTypeDefinition) TypesById[eventsTypeIndex].Definition;

        Variant eventVariant;

        try
        {
            eventVariant = events.Variants.Find(eventIndex);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Event with index '{eventIndex}' was not found in pallet '{pallet.Name}' (index={moduleIndex}).");
        }

        return (pallet, eventVariant);
    }

    public (PalletMetadata pallet, Variant @event) FindEvent(byte[] index)
    {
        if (index is not { Length: 2 })
        {
            throw new ArgumentException(
                "Index must be an array of length 2.");
        }

        return FindEvent(index[0], index[1]);
    }

    public (PalletMetadata pallet, Variant variant) FindPalletCallVariant(int palletIndex, int callIndex)
    {
        var pallet = FindPallet(palletIndex);

        var callType = TypesById[pallet.Calls.Type];

        if (callType.Definition is VariantTypeDefinition variantType)
        {
            var variant = variantType.Variants.Find(callIndex);

            if (variant == null)
            {
                throw new InvalidOperationException(
                    $"Variant '{callIndex}' was not found in pallet '{pallet.Name}'.");
            }

            return (pallet, variant);
        }

        throw new InvalidOperationException($"Call definition is not a variant; type={callType}.");
    }

    public (PalletMetadata pallet, Variant variant) FindPalletCallVariant(string palletName, string callName)
    {
        var pallet = FindPallet(palletName);

        var callType = TypesById[pallet.Calls.Type];
        
        if (callType.Definition is VariantTypeDefinition variantType)
        {
            var variant = variantType.Variants.SingleOrDefault(
                x => string.Equals(x.Name, callName, StringComparison.OrdinalIgnoreCase));

            if (variant == null)
            {
                throw new InvalidOperationException(
                    $"Variant '{callName}' was not found in pallet '{palletName}'.");
            }

            return (pallet, variant);
        }

        throw new InvalidOperationException($"Call definition is not a variant; type={callType}.");
    }

    public PortableType GetTypeByFullName(string path)
    {
        // TODO: cache

        return TypesById.Values
            .First(x => x.FullName == path);
    }

    public void VerifySignature(
        Variant variant,
        params Expression<Func<Field, PortableType, bool>>[] assertions)
    {
        if (variant == null)
        {
            throw new ArgumentNullException(nameof(variant));
        }

        if (assertions is not { Length: not 0 })
        {
            throw new ArgumentException(
                "No asserts were provided",
                nameof(assertions));
        }

        for (int index = 0; index < variant.Fields.Count; ++index)
        {
            // TODO: cache

            var expression = assertions[index];
            var assertion = expression.Compile();

            var field = variant.Fields[index];
            var type = TypesById[field.Type];

            if (!assertion(field, type))
            {
                throw new RuntimeAssumptionFailedException(expression.ToString());
            }
        }
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

    class RuntimeAssumptionFailedException : Exception
    {
        public RuntimeAssumptionFailedException(string assumption)
            : base($"Runtime assumption failed: {assumption}")
        { }
    }
}