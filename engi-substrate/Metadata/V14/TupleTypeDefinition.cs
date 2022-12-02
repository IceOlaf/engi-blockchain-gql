namespace Engi.Substrate.Metadata.V14;

public class TupleTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Tuple;

    public TType[] Fields { get; set; } = null!;

    internal new static TupleTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Fields = stream.ReadList(TType.Parse)
        };
    }
}