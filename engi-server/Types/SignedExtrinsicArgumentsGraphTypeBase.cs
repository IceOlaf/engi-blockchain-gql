using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public abstract class SignedExtrinsicArgumentsGraphTypeBase<T> : InputObjectGraphType<T>
    where T : SignedExtrinsicArgumentsBase
{
    protected SignedExtrinsicArgumentsGraphTypeBase()
    {
        Field(x => x.SenderSecret)
            .Description("The private key of the executor.");
        Field(x => x.Tip)
            .Description("The tip to include in the transaction.");
    }
}