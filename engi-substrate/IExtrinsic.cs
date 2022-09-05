using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public interface IExtrinsic : IScaleSerializable
{
    string PalletName { get; }

    string CallName { get; }

    IEnumerable<Func<Field, PortableType, PortableType?, bool>> GetVariantAssertions();
}

public static class ExtrinsicExtensions
{
    public static PalletCallIndex VerifySignature(
        this IExtrinsic extrinsic,
        RuntimeMetadata metadata)
    {
        var (_, variant) = metadata
            .FindPalletCallVariant(extrinsic);

        metadata.VerifySignature(variant, extrinsic.GetVariantAssertions().ToArray());

        return variant.CallIndex;
    }
}