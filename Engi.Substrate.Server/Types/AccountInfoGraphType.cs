using Engi.Substrate.Pallets;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AccountInfoGraphType : ObjectGraphType<AccountInfo>
{
    public AccountInfoGraphType()
    {
        Field(x => x.Nonce);
        Field(x => x.Consumers);
        Field(x => x.Providers);
        Field(x => x.Sufficients);
        Field(x => x.Data);
    }
}