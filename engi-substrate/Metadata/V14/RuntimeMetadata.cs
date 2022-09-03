namespace Engi.Substrate.Metadata.V14;

public class RuntimeMetadata
{
    public int MagicNumber { get; set; }

    public int Version { get; set; }

    public Dictionary<ulong, PortableType> TypesById { get; set; } = null!;

    public PalletMetadataCollection Pallets { get; set; } = null!;

    public ExtrinsicMetadata Extrinsic { get; set; } = null!;

    public TType TypeId { get; set; } = null!;

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

        if (pallet.Calls == null)
        {
            throw new ArgumentException("Pallet does not define any calls.", nameof(palletIndex));
        }

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

        if (pallet.Calls == null)
        {
            throw new ArgumentException("Pallet does not define any calls.", nameof(palletName));
        }

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
        params Func<Field, PortableType, PortableType?, bool>[] assertions)
    {
        if (variant == null)
        {
            throw new ArgumentNullException(nameof(variant));
        }

        if (assertions.Length != variant.Fields.Count)
        {
            throw new ArgumentException(
                $"One assertion per field must be provided; expected={variant.Fields.Count} actual={assertions.Length}",
                nameof(assertions));
        }

        for (int index = 0; index < variant.Fields.Count; ++index)
        {
            // TODO: cache

            var assertion = assertions[index];

            var field = variant.Fields[index];
            var type = TypesById[field.Type];
            var innerType = type.Definition is IHasInnerType inner ? TypesById[inner.Type] : null;

            if (!assertion(field, type, innerType))
            {
                throw new RuntimeAssumptionFailedException($"Assertion for field index={index} failed.");
            }
        }
    }

    private void PopulateReferences(TType type)
    {
        if (type.Reference != null)
        {
            return;
        }

        type.Reference = TypesById[type];

        foreach (var param in type.Reference.Params)
        {
            if (param.Type != null)
            {
                PopulateReferences(param.Type);
            }
        }

        if (type.Reference.Definition is IHasInnerType inner)
        {
            PopulateReferences(inner.Type);
        }
        else if (type.Reference.Definition is VariantTypeDefinition variantType)
        {
            foreach (var variant in variantType.Variants)
            {
                foreach (var field in variant.Fields)
                {
                    PopulateReferences(field.Type);
                }
            }
        } 
        else if (type.Reference.Definition is CompositeTypeDefinition compositeType)
        {
            foreach (var field in compositeType.Fields)
            {
                PopulateReferences(field.Type);
            }
        }
        else if (type.Reference.Definition is TupleTypeDefinition tupleType)
        {
            foreach (var field in tupleType.Fields)
            {
                PopulateReferences(field);
            }
        }
    }

    private RuntimeMetadata PopulateReferences()
    {
        foreach (var type in TypesById.Values)
        {
            if (type.Definition is IHasInnerType inner)
            {
                PopulateReferences(inner.Type);
            }
        }

        foreach (var pallet in Pallets)
        {
            if (pallet.Calls != null)
            {
                PopulateReferences(pallet.Calls.Type);
            }

            foreach (var constant in pallet.Constants)
            {
                PopulateReferences(constant.Type);
            }

            if (pallet.Errors != null)
            {
                PopulateReferences(pallet.Errors.Type);
            }

            if (pallet.Events != null)
            {
                PopulateReferences(pallet.Events.Type);
            }

            if (pallet.Storage != null)
            {
                foreach (var storage in pallet.Storage.Items)
                {
                    switch (storage.Type)
                    {
                        case StorageEntryPlain plain:
                            PopulateReferences(plain.Value);
                            break;
                        
                        case StorageEntryMap map:
                            PopulateReferences(map.Key);
                            PopulateReferences(map.Value);
                            break;
                    }
                }
            }
        }

        PopulateReferences(TypeId);

        return this;
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

        return new RuntimeMetadata()
        {
            MagicNumber = magicNumber,
            Version = version,
            TypesById = stream.ReadList(PortableType.Parse)
                .ToDictionary(x => x.Id, x => x),
            Pallets = new PalletMetadataCollection(stream.ReadList(PalletMetadata.Parse)),
            Extrinsic = ExtrinsicMetadata.Parse(stream),
            TypeId = TType.Parse(stream)
        }.PopulateReferences();
    }

    class RuntimeAssumptionFailedException : Exception
    {
        public RuntimeAssumptionFailedException(string assumption)
            : base($"Runtime assumption failed: {assumption}")
        { }
    }
}