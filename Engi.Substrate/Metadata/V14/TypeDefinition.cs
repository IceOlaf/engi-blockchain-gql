namespace Engi.Substrate.Metadata.V14;

public abstract class TypeDefinition
{
    public abstract TypeDefinitionType DefinitionType { get; }

    public static TypeDefinition? Parse(ScaleStream stream)
    {
        var type = stream.ReadEnum<TypeDefinitionType>();

        return type switch
        {
            TypeDefinitionType.Composite => CompositeTypeDefinition.Parse(stream),
            TypeDefinitionType.Variant => VariantTypeDefinition.Parse(stream),
            TypeDefinitionType.Sequence => SequenceTypeDefinition.Parse(stream),
            TypeDefinitionType.Array  => ArrayTypeDefinition.Parse(stream),
            TypeDefinitionType.Tuple => TupleTypeDefinition.Parse(stream),
            TypeDefinitionType.Primitive => PrimitiveTypeDefinition.Parse(stream),
            TypeDefinitionType.Compact => CompactTypeDefinition.Parse(stream),
            TypeDefinitionType.BitSequence => BitSequenceTypeDefinition.Parse(stream),
            TypeDefinitionType.Void => null,
            _ => throw new NotImplementedException(type.ToString())
        };
    }
}