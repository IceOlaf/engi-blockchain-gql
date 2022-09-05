namespace Engi.Substrate.Metadata.V14;

public class CallVariant
{
    public string Name { get; init; }
    public FieldCollection Fields { get; init; }
    public PalletCallIndex CallIndex { get; init; }
    public string?[]? Docs { get; init; }

    public override string ToString()
    {
        return Name;
    }

    public byte PalletIndex { get; init; }

    public CallVariant(PalletMetadata pallet, Variant variant)
    {
        PalletIndex = pallet.Index;
        Name = variant.Name;
        Fields = variant.Fields;
        CallIndex = new()
        {
            PalletIndex = pallet.Index,
            CallIndex = variant.Index
        };
        Docs = variant.Docs;
    }
}