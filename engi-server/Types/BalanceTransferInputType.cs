namespace Engi.Substrate.Server.Types;

public class BalanceTransferInputType : SignedExtrinsicInputTypeBase<BalanceTransferInput>
{
    public BalanceTransferInputType()
    {
        Field(x => x.RecipientAddress);
        Field(x => x.Amount);
    }
}