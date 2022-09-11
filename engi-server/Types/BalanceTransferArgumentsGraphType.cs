using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class BalanceTransferArgumentsGraphType : InputObjectGraphType<BalanceTransferArguments>
{
    public BalanceTransferArgumentsGraphType()
    {
        Description = "Arguments for the balance_transfer signed extrinsic.";

        Field(x => x.Destination, type: typeof(AddressGraphType))
            .Description("The address of the recipient.");
        Field(x => x.Amount)
            .Description("The amount to transfer.");
    }
}
