namespace Engi.Substrate.Metadata.V14;

public class VariantCollection : List<Variant>
{
    public VariantCollection()
    { }

    public VariantCollection(IEnumerable<Variant> variants)
        : base(variants)
    { }

    public Variant Find(byte index)
    {
        return this.Single(x => x.Index == index);
    }

    public Variant Find(string name)
    {
        return this.Single(x => x.Name == name);
    }

    public byte IndexOf(string name)
    {
        return Find(name).Index;
    }
}