using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class LowerCaseEnumerationGraphType<T> : EnumerationGraphType<T> where T : Enum
{
    protected override string ChangeEnumCase(string val)
    {
        return val.ToLower();
    }
}
