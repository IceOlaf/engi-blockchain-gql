using System.Numerics;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class BigIntegerType : ScalarGraphType
{
    public override object? ParseValue(object? value)
    {
        if (value is string s)
        {
            return BigInteger.Parse(s);
        }

        return (BigInteger) Convert.ToUInt64(value);
    }
}