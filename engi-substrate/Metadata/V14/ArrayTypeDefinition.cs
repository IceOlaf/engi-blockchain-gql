namespace Engi.Substrate.Metadata.V14;

public class ArrayTypeDefinition : TypeDefinition, IHasInnerType
{
    public override TypeDefinitionType DefinitionType => TypeDefinitionType.Array;

    public uint Len { get; set; }
        
    public TType Type { get; set; } = null!;

    internal new static ArrayTypeDefinition Parse(ScaleStreamReader stream)
    {
        return new()
        {
            Len = stream.ReadUInt32(),
            Type = TType.Parse(stream)
        };
    }
}