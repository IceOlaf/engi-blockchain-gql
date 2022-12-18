using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AddressGraphType : ScalarGraphType
{
    public override object? ParseValue(object? value)
    {
        if (value is Address address)
        {
            return address;
        }

        if(value is string s)
        {
            return Address.Parse(s);
        }

        return null;
    }
}
