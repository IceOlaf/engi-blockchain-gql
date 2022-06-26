using System.Dynamic;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public static class ScaleStreamReaderExtensions
{
    public static ExpandoObject DeserializeDynamicType(this ScaleStreamReader reader, Variant variant, RuntimeMetadata meta)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (variant == null)
        {
            throw new ArgumentNullException(nameof(variant));
        }

        return DeserializeFields(reader, variant.Fields, meta);
    }

    private static ExpandoObject DeserializeFields(
        ScaleStreamReader reader, 
        FieldCollection fields,
        RuntimeMetadata meta)
    {
        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object>)expando;

        for (int index = 0; index < fields.Count; index++)
        {
            var x = fields[index];
            dictionary.Add(x.Name ?? $"field[{index}]", DeserializeField(reader, x, meta));
        }

        return expando;
    }

    private static object DeserializeField(ScaleStreamReader reader, Field field, RuntimeMetadata meta)
    {
        var fieldType = meta.TypesById[field.Type];

        if (field.TypeName == "T::AccountId")
        {
            byte[] address = reader.ReadFixedSizeByteArray(32);
            
            return Address.From(address).Id;
        }

        return fieldType.Definition switch
        {
            ArrayTypeDefinition arrayType => DeserializeArray(reader, arrayType, meta),
            CompositeTypeDefinition compositeType => DeserializeComposite(reader, compositeType, meta),
            PrimitiveTypeDefinition primitiveType => reader.ReadPrimitive(primitiveType.PrimitiveType),
            VariantTypeDefinition variantType => DeserializeVariantAsEnum(reader, variantType),
            _ => throw new NotSupportedException()
        };
    }

    private static object DeserializeArray(
        ScaleStreamReader reader, 
        ArrayTypeDefinition typeDef, 
        RuntimeMetadata meta)
    {
        var type = (PrimitiveTypeDefinition)meta.TypesById[typeDef.Type].Definition;

        return type.PrimitiveType switch
        {
            PrimitiveType.UInt8 => reader.ReadFixedSizeByteArray((int)typeDef.Len),
            _ => throw new NotImplementedException($"Reading arrays of type '{type.PrimitiveType}' is not implemented.")
        };
    }

    private static object DeserializeComposite(
        ScaleStreamReader reader,
        CompositeTypeDefinition typeDef,
        RuntimeMetadata meta)
    {
        if (typeDef.Fields.Count == 1)
        {
            return DeserializeField(reader, typeDef.Fields[0], meta);
        }

        return DeserializeFields(reader, typeDef.Fields, meta);
    }

    private static string DeserializeVariantAsEnum(ScaleStreamReader reader, VariantTypeDefinition typeDef)
    {
        byte index = (byte) reader.ReadByte();

        return typeDef.Variants.Find(index).Name;
    }
    
}