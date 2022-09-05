using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class BalanceTransferArguments : IExtrinsic
{
    private readonly byte addressType;

    public BalanceTransferArguments(RuntimeMetadata meta)
    {
        addressType = meta.MultiAddressTypeDefinition.Variants.IndexOf("Id");
    }

    public string PalletName => ChainKeys.Balances.Name;
    public string CallName => ChainKeys.Balances.Calls.Transfer;

    [Required]
    public Address Destination { get; set; } = null!;

    // TODO: validate
    public BigInteger Amount { get; set; }

    public void Serialize(ScaleStreamWriter writer)
    {
        writer.Write(addressType);
        writer.Write(Destination);
        writer.WriteCompact(Amount);
    }

    public IEnumerable<Func<Field, PortableType, PortableType?, bool>> GetVariantAssertions()
    {
        return new Func<Field, PortableType, PortableType?, bool>[]
        {
            (field, type, _) => field.Name == "dest" && type.FullName == "sp_runtime:multiaddress:MultiAddress",
            (field, type, _) => field.Name == "value" && type.Definition is CompactTypeDefinition
        };
    }
}