namespace Engi.Substrate.Server.Types;

public class BalanceTransferArgumentsGraphType : SignedExtrinsicArgumentsGraphTypeBase<BalanceTransferArguments>
{
    public BalanceTransferArgumentsGraphType()
    {
        Description = "Arguments for the balance_transfer signed extrinsic.";

        Field(x => x.RecipientAddress)
            .Description("The address of the recipient.");
        Field(x => x.Amount)
            .Description("The amount to transfer.");
    }
}