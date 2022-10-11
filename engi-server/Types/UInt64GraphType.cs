using System.Numerics;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UInt64GraphType : ScalarGraphType
{
    public override object? ParseValue(object? value)
    {
        var parsed = value is string s ? BigInteger.Parse(s) : Convert.ToUInt64(value);

        if (parsed.GetByteCount(true) > 8)
        {
            throw new InvalidDataException("Value exceeds limit of 64 bits.");
        }

        if (parsed.Sign == -1)
        {
            throw new InvalidDataException("Negative value supplied for an unsigned type.");
        }

        return (ulong) parsed;
    }
}