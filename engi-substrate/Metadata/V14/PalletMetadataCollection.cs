namespace Engi.Substrate.Metadata.V14;

public class PalletMetadataCollection : List<PalletMetadata>
{
    public PalletMetadataCollection() { }

    public PalletMetadataCollection(IEnumerable<PalletMetadata> pallets)
        : base(pallets)
    { }
}