namespace Engi.Substrate.Metadata.V14;

public class CompositeTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Composite;

    public Field[]? Fields { get; set; }

    internal new static CompositeTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Fields = stream.ReadList(Field.Parse)
        };
    }
}