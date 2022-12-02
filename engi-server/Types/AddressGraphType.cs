using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AddressGraphType : ScalarGraphType
{
    public override object? ParseValue(object? value)
    {
        string? s = (string?)value;

        if (s == null)
        {
            return null;
        }

        return Address.Parse(s);
    }
}