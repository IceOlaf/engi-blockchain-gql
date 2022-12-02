using Engi.Substrate.Pallets;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AccountDataGraphType : ObjectGraphType<AccountData>
{
    public AccountDataGraphType()
    {
        Description = "All balance information for an account.";

        Field(x => x.Free)
            .Description("Non-reserved part of the balance. There may still be restrictions on this, but it is the total pool what may in principle be transferred, reserved and used for tipping.");
        Field(x => x.Reserved)
            .Description("This balance is a 'reserve' balance that other subsystems use in order to set aside tokens that are still 'owned' by the account holder, but which are suspendable.");
        Field(x => x.FeeFrozen)
            .Description("The amount that `free` may not drop below when withdrawing for *anything except transaction fee payment.");
        Field(x => x.MiscFrozen)
            .Description("The amount that `free` may not drop below when withdrawing specifically for transaction fee payment.");
    }
}