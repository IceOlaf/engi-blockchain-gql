using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public sealed class SudoCallArguments : IExtrinsic
{
    private readonly IExtrinsic callExtrinsic;

    public SudoCallArguments(IExtrinsic callExtrinsic)
    {
        this.callExtrinsic = callExtrinsic;
    }

    public string PalletName => ChainKeys.Sudo.Name;
    public string CallName => ChainKeys.Sudo.Calls.Call;

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata meta)
    {
        var (_, variant) = meta.FindPalletCallVariant(callExtrinsic);

        var callIndex = variant.CallIndex;

        writer.Write(callIndex, meta);
        writer.Write(callExtrinsic, meta);
    }
    
    public IEnumerable<Func<Field, PortableType, PortableType?, bool>> GetVariantAssertions()
    {
        return new Func<Field, PortableType, PortableType?, bool>[]
        {
            (field, _, _) => field.Name == "call"
        };
    }
}