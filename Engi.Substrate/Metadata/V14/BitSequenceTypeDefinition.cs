namespace Engi.Substrate.Metadata.V14;

public class BitSequenceTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.BitSequence;

    public TType? StoreType { get; set; }

    public TType? OrderType { get; set; }

    internal new static BitSequenceTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            StoreType = TType.Parse(stream),
            OrderType = TType.Parse(stream)
        };
    }
}