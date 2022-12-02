namespace Engi.Substrate.Metadata.V14;

public class FieldCollection : List<Field>
{
    public FieldCollection()
    { }

    public FieldCollection(IEnumerable<Field> fields)
        : base(fields)
    { }

    public Field? Find(string name)
    {
        return this.SingleOrDefault(
            x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
    }
}