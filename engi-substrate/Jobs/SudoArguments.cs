using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Jobs;

public sealed class SudoCallArguments : IExtrinsic
{
    private readonly IExtrinsic callExtrinsic;
    private readonly PalletCallIndex callIndex;

    public SudoCallArguments(IExtrinsic callExtrinsic, RuntimeMetadata meta)
    {
        this.callExtrinsic = callExtrinsic;
        
        var (_, variant) = meta.FindPalletCallVariant(callExtrinsic);

        callIndex = variant.CallIndex;
    }

    public string PalletName => ChainKeys.Sudo.Name;
    public string CallName => ChainKeys.Sudo.Calls.Call;

    public void Serialize(ScaleStreamWriter writer)
    {
        writer.Write(callIndex);
        writer.Write(callExtrinsic);
    }
    
    public IEnumerable<Func<Field, PortableType, PortableType?, bool>> GetVariantAssertions()
    {
        return new Func<Field, PortableType, PortableType?, bool>[]
        {
            (field, _, _) => field.Name == "call"
        };
    }
}