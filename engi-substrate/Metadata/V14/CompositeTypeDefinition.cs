namespace Engi.Substrate.Metadata.V14;

public class CompositeTypeDefinition : TypeDefinition
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Composite;

    public FieldCollection Fields { get; set; } = null!;

    internal new static CompositeTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Fields = new FieldCollection(stream.ReadList(Field.Parse))
        };
    }
}