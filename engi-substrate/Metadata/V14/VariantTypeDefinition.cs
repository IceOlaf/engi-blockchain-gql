namespace Engi.Substrate.Metadata.V14;

public class VariantTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Variant;

    public VariantCollection Variants { get; set; } = null!;

    internal new static VariantTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Variants = new VariantCollection(stream.ReadList(Variant.Parse))
        };
    }
}