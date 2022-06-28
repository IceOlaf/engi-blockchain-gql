using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class BalanceTransferInputType : InputObjectGraphType<BalanceTransferInput>
{
    public BalanceTransferInputType()
    {
        Field(x => x.SenderSecret);
        Field(x => x.RecipientAddress);
        Field(x => x.Amount);
        Field(x => x.Tip);
    }
}