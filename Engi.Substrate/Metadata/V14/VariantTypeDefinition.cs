namespace Engi.Substrate.Metadata.V14;

public class VariantTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Variant;

    public Variant[]? Variants { get; set; }

    internal new static VariantTypeDefinition Parse(ScaleStream stream)
    {
        return new()
        {
            Variants = stream.ReadList(Variant.Parse)
        };
    }
}