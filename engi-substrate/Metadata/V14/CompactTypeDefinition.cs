namespace Engi.Substrate.Metadata.V14;

public class CompactTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Compact;

    public TType? Type { get; set; }

    internal new static CompactTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Type = TType.Parse(stream)
        };
    }
}