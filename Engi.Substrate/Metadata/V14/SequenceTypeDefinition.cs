namespace Engi.Substrate.Metadata.V14;

public class SequenceTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Sequence;
    public TType? Type { get; set; }

    internal new static SequenceTypeDefinition Parse(ScaleStream stream)
    {
        return new()
        {
            Type = TType.Parse(stream)
        };
    }
}