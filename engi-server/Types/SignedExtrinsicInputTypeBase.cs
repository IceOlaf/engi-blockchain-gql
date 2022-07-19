using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public abstract class SignedExtrinsicInputTypeBase<T> : InputObjectGraphType<T>
    where T : SignedExtrinsicInputBase
{
    protected SignedExtrinsicInputTypeBase()
    {
        Field(x => x.SenderSecret);
        Field(x => x.Tip);
    }
}