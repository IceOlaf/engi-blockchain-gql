using Engi.Substrate.Metadata.V14;
using System.Numerics;
using System.Text;

namespace Engi.Substrate;

public static class ScaleStreamReaderExtensions
{
    public static BigInteger Deserialize(
        this ScaleStreamReader reader,
        CompactTypeDefinition _)
    {
        return reader.ReadCompactBigInteger();
    }

    public static object Deserialize(
        this ScaleStreamReader reader,
        ArrayTypeDefinition typeDef,
        RuntimeMetadata meta,
        Func<byte[], object> byteArrayConverter)
    {
        var itemType = meta.TypesById[typeDef.Type];

        if (itemType.FullName == "u8")
        {
            var data = reader.ReadFixedSizeByteArray(typeDef.Len);

            return byteArrayConverter(data);
        }

        throw new NotImplementedException($"Reading arrays of typeId='{itemType.Id}' is not implemented.");
    }

    public static object Deserialize(
        this ScaleStreamReader reader,
        ArrayTypeDefinition typeDef,
        RuntimeMetadata meta)
    {
        return Deserialize(reader, typeDef, meta, Hex.GetString0X);
    }

    public static object Deserialize(
        this ScaleStreamReader reader,
        CompositeTypeDefinition typeDef,
        RuntimeMetadata meta)
    {
        if (typeDef.Fields.Count == 1)
        {
            return Deserialize(reader, typeDef.Fields[0], meta);
        }

        return Deserialize(reader, typeDef.Fields, meta);
    }

    public static object Deserialize(
        this ScaleStreamReader reader,
        BitSequenceTypeDefinition typeDef,
        RuntimeMetadata meta)
    {
        var storeType = meta.TypesById[typeDef.StoreType].Definition as PrimitiveTypeDefinition;
        var orderType = meta.TypesById[typeDef.OrderType];

        if (storeType?.PrimitiveType != PrimitiveType.UInt8)
        {
            throw new NotImplementedException("Bit sequence that's not u8");
        }

        if (orderType.Path.Last() != "Lsb0")
        {
            throw new NotImplementedException("Bit sequence with MSB order");
        }

        ulong bitLength = reader.ReadCompactInteger();
        uint bufferLength = (uint)Math.Ceiling(bitLength / 8.0);

        if (bufferLength > 1)
        {
            throw new NotImplementedException("Bit sequence with more than one bytes");
        }

        byte[] data = reader.ReadFixedSizeByteArray(bufferLength);

        return "0b" + Convert.ToString(data[0], 2).PadRight(8, '0');
    }

    public static object Deserialize(
        this ScaleStreamReader reader,
        SequenceTypeDefinition typeDef,
        RuntimeMetadata meta)
    {
        var itemType = meta.TypesById[typeDef.Type];

        if (itemType.FullName == "u8")
        {
            var data = reader.ReadByteArray();

            return Hex.GetString0X(data);
        }

        return reader.ReadList(r => Deserialize(r, itemType, meta));
    }

    public static Dictionary<int, object> Deserialize(
        this ScaleStreamReader reader,
        TupleTypeDefinition typeDef,
        RuntimeMetadata meta)
    {
        var result = new Dictionary<int, object>();

        for (int i = 0; i < typeDef.Fields!.Length; ++i)
        {
            var type = meta.TypesById[typeDef.Fields[i].Value];

            result[i] = Deserialize(reader, type, meta);
        }

        return result;
    }

    public static object Deserialize(
        this ScaleStreamReader reader,
        PortableType type,
        RuntimeMetadata meta)
    {
        if (type.Definition is CompactTypeDefinition c)
        {
            return Deserialize(reader, c);
        }

        if (type.Definition is CompositeTypeDefinition composite)
        {
            return Deserialize(reader, composite, meta);
        }

        if (type.Definition is ArrayTypeDefinition array)
        {
            return Deserialize(reader, array, meta);
        }

        if (type.Definition is SequenceTypeDefinition seq)
        {
            return Deserialize(reader, seq, meta);
        }

        if (type.Definition is BitSequenceTypeDefinition bitSequence)
        {
            return Deserialize(reader, bitSequence, meta);
        }

        if (type.Definition is PrimitiveTypeDefinition primitive)
        {
            return reader.ReadPrimitive(primitive.PrimitiveType);
        }

        if (type.Definition is TupleTypeDefinition tuple)
        {
            return Deserialize(reader, tuple, meta);
        }

        if (type.Definition is VariantTypeDefinition variant)
        {
            int index = reader.ReadByte();

            var variantType = variant.Variants.Find(index);

            if (!variantType.Fields.Any())
            {
                // enums

                return variantType.Name;
            }

            var fields = variantType.Fields
                .Select(field => Deserialize(reader, field, meta))
                .ToArray();

            if (fields.Length == 1)
            {
                return new Dictionary<string, object>
                {
                    [variantType.Name] = fields[0]
                };
            }

            return new Dictionary<string, object>
            {
                [variantType.Name] = fields
            };
        }

        throw new NotImplementedException();
    }

    public static object Deserialize(
        this ScaleStreamReader reader, 
        FieldCollection fields,
        RuntimeMetadata meta)
    {
        if (fields.Count == 1)
        {
            return Deserialize(reader, fields.First(), meta);
        }

        // not sure if there is a better way to do this - can't find an indication
        // if an event is an object or an array

        bool indexByName = fields.Any(x => !string.IsNullOrEmpty(x.Name));

        if (indexByName)
        {
            var value = new Dictionary<string, object>();

            foreach (var field in fields)
            {
                var fieldType = meta.TypesById[field.Type];

                if (string.IsNullOrEmpty(field.Name))
                {
                    throw new NotImplementedException(
                        $"Field without a name in type id={fieldType.Id}");
                }

                value[field.Name] = Deserialize(reader, field, meta);
            }

            return value;
        }
        else
        {
            // index as array

            var value = new Dictionary<int, object>();

            for (int index = 0; index < fields.Count; ++index)
            {
                var field = fields[index];

                value[index] = Deserialize(reader, field, meta);
            }

            return value;
        }
    }

    public static object Deserialize(
        this ScaleStreamReader reader, 
        Field field, 
        RuntimeMetadata meta)
    {
        var fieldType = meta.TypesById[field.Type];

        if (fieldType.FullName == "sp_core:crypto:AccountId32")
        {
            byte[] address = reader.ReadFixedSizeByteArray(32);

            return Address.From(address).Id;
        }

        if (field.TypeName == "ConsensusEngineId")
        {
            return Deserialize(
                reader,
                (ArrayTypeDefinition)fieldType.Definition,
                meta,
                Encoding.UTF8.GetString);
        }

        if (field.TypeName == "T::Moment")
        {
            long value = (long) reader.ReadCompactBigInteger();

            return DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
        }

        if (fieldType.FullName == "sp_runtime:multiaddress:MultiAddress")
        {
            return MultiAddress.Parse(reader, meta);
        }

        return Deserialize(reader, fieldType, meta);
    }
}