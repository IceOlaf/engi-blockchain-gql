using Engi.Substrate.Pallets;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AccountInfoGraphType : ObjectGraphType<AccountInfo>
{
    public AccountInfoGraphType()
    {
        Description = "Information of an account.";

        Field(x => x.Nonce)
            .Description("The number of transactions this account has sent.");
        Field(x => x.Consumers)
            .Description("The number of other modules that currently depend on this account's existence. The account cannot be reaped until this is zero.");
        Field(x => x.Providers)
            .Description("The number of other modules that allow this account to exist. The account may not be reaped until this is zero.");
        Field(x => x.Sufficients)
            .Description("The number of modules that allow this account to exist for their own purposes only. The account may not be reaped until this and `providers` are both zero.");
        Field(x => x.Data, type: typeof(AccountDataGraphType))
            .Description("The additional data that belongs to this account. Used to store the balance(s) in a lot of chains.");
    }
}