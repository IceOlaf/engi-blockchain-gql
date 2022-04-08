namespace Engi.Substrate.Metadata.V11;

public static class MetadataParser
{
    public static Metadata Parse(ScaleStream stream)
    {
        const int metadataMagicNumber = 0x6174656D;

        int magicNumber = stream.ReadInt32();

        if (magicNumber != metadataMagicNumber)
        {
            throw new InvalidDataException(
                $"Expected magic number 0x{metadataMagicNumber:x} but found 0x{magicNumber:x}");
        }

        int version = stream.ReadByte();

        if (version != 11)
        {
            throw new InvalidDataException("Only know how to parse v11 right now");
        }

        var modules = stream.ReadList(stream =>
        {
            var module = new ModuleMetadata
            {
                Name = stream.ReadString()
            };

            if (stream.ReadBool())
            {
                module.Storage = new StorageMetadata
                {
                    Prefix = stream.ReadString(),
                    Items = stream.ReadList(ParseStorageEntryMetadata)
                };
            }

            if (stream.ReadBool())
            {
                module.Calls = stream.ReadList(ParseFunctionMetadata);
            }

            if (stream.ReadBool())
            {
                module.Events = stream.ReadList(ParseEventMetadata);
            }

            module.Constants = stream.ReadList(ParseModuleConstantMetadata);
            module.Errors = stream.ReadList(ParseErrorMetadata);

            return module;
        });

        var extrinsic = new ExtrinsicMetadata
        {
            Version = stream.ReadByte(),
            SignedExtensions = stream.ReadList(s => s.ReadString())
        };

        return new()
        {
            MagicNumber = magicNumber,
            Version = version,
            Modules = modules,
            Extrinsic = extrinsic
        };
    }

    private static ErrorMetadata ParseErrorMetadata(ScaleStream stream)
    {
        return new()
        {
            Name = stream.ReadString(),
            Documentation = stream.ReadList(s => s.ReadString())
        };
    }

    private static ModuleConstantMetadata ParseModuleConstantMetadata(ScaleStream stream)
    {
        return new()
        {
            Name = stream.ReadString(),
            Ty = stream.ReadString(),
            Value = stream.ReadList(s => (byte)s.ReadByte()),
            Documentation = stream.ReadList(s => s.ReadString())
        };
    }

    private static EventMetadata ParseEventMetadata(ScaleStream stream)
    {
        return new()
        {
            Name = stream.ReadString(),
            Arguments = stream.ReadList(s => s.ReadString()),
            Documentation = stream.ReadList(s => s.ReadString())
        };
    }

    private static FunctionMetadata ParseFunctionMetadata(ScaleStream stream)
    {
        return new()
        {
            Name = stream.ReadString(),
            Arguments = stream.ReadList(ParseFunctionArgumentMetadata),
            Documentation = stream.ReadList(s => s.ReadString())
        };
    }

    private static FunctionArgumentMetadata ParseFunctionArgumentMetadata(ScaleStream stream)
    {
        return new()
        {
            Name = stream.ReadString(),
            Ty = stream.ReadString()
        };
    }

    private static StorageEntryMetadata ParseStorageEntryMetadata(ScaleStream stream)
    {
        var item = new StorageEntryMetadata
        {
            Name = stream.ReadString()
        };

        item.Modifier = stream.ReadEnum<StorageEntryModifier>();
        item.TyType = stream.ReadEnum<StorageEntryType>();

        item.Type = item.TyType switch
        {
            StorageEntryType.Plain => new StorageEntryPlain
            {
                Value = stream.ReadString()
            },
            StorageEntryType.Map => new StorageEntryMap
            {
                Hasher = stream.ReadEnum<StorageHasher>(),
                Key = stream.ReadString(),
                Value = stream.ReadString(),
                Linked = stream.ReadBool()
            },
            StorageEntryType.DoubleMap => new StorageEntryDoubleMap
            {
                Hasher = stream.ReadEnum<StorageHasher>(),
                Key1 = stream.ReadString(),
                Key2 = stream.ReadString(),
                Value = stream.ReadString(),
                Key2Hasher = stream.ReadEnum<StorageHasher>()
            },
            StorageEntryType.NMap => new StorageEntryNMap
            {
                Keys = stream.ReadList(s => s.ReadString()),
                Hashers = stream.ReadList(s => s.ReadEnum<StorageHasher>()),
                Value = stream.ReadString()
            },
            _ => throw new NotImplementedException(item.TyType.ToString())
        };

        item.Fallback = stream.ReadList(s => (byte)s.ReadByte());
        item.Docs = stream.ReadList(s => s.ReadString());

        return item;
    }
}