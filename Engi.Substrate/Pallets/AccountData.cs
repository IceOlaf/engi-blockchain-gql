using System.Numerics;

namespace Engi.Substrate.Pallets;

/// <see cref="https://crates.parity.io/pallet_balances/struct.AccountData.html"/>
public class AccountData
{
    public BigInteger Free { get; set; }

    public BigInteger Reserved { get; set; }

    public BigInteger MiscFrozen { get; set; }

    public BigInteger FeeFrozen { get; set; }

    public static AccountData Parse(ScaleStream stream)
    {
        return new()
        {
            Free = stream.ReadUInt128(),
            Reserved = stream.ReadUInt128(),
            MiscFrozen = stream.ReadUInt128(),
            FeeFrozen = stream.ReadUInt128()
        };
    }
}