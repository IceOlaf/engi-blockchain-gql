namespace Engi.Substrate.Metadata.V14;

public class SequenceTypeDefinition : TypeDefinition, IHasInnerType
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Sequence;
    public TType Type { get; set; } = null!;

    internal new static SequenceTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Type = TType.Parse(stream)
        };
    }
}