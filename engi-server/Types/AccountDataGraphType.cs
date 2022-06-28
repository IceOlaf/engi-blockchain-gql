using Engi.Substrate.Pallets;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AccountDataGraphType : ObjectGraphType<AccountData>
{
    public AccountDataGraphType()
    {
        Field(x => x.Free);
        Field(x => x.Reserved);
        Field(x => x.FeeFrozen);
        Field(x => x.MiscFrozen);
    }
}