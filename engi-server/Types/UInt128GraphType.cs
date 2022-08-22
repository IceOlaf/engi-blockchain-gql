using System.Numerics;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UInt128GraphType : ScalarGraphType
{
    public override object? ParseValue(object? value)
    {
        var parsed = value is string s ? BigInteger.Parse(s) : Convert.ToUInt64(value);

        if (parsed.GetByteCount(true) > 16)
        {
            throw new InvalidDataException("Value exceeds limit of 128 bits.");
        }

        if (parsed.Sign == -1)
        {
            throw new InvalidDataException("Negative value supplied for an unsigned type.");
        }

        return parsed;
    }
}