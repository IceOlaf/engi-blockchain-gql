namespace Engi.Substrate.Server.Types;

public class BalanceTransferArgumentsType : SignedExtrinsicArgumentsGraphTypeBase<BalanceTransferArguments>
{
    public BalanceTransferArgumentsType()
    {
        Field(x => x.RecipientAddress)
            .Description("The address of the recipient.");
        Field(x => x.Amount)
            .Description("The amount to transfer.");
    }
}