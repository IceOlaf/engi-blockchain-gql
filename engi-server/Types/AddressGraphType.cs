using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AddressGraphType : ScalarGraphType
{
    public override object? ParseValue(object? value)
    {
        var address = (Address?)value;

        return address?.Id;
    }
}