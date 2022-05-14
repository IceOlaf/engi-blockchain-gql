namespace Engi.Substrate.Metadata.V14;

public class PrimitiveTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Primitive;

    public PrimitiveType PrimitiveType { get; set; }

    internal new static PrimitiveTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            PrimitiveType = stream.ReadEnum<PrimitiveType>()
        };
    }
}