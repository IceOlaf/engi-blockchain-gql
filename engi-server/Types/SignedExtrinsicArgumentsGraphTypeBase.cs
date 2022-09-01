using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public abstract class SignedExtrinsicArgumentsGraphTypeBase<T> : InputObjectGraphType<T>
    where T : ISignedExtrinsic
{
    protected SignedExtrinsicArgumentsGraphTypeBase()
    {
        Field(x => x.SenderKeypairPkcs8)
            .Description("The PKCS8 keypair of the executor, base64-encoded, as returned by `createUser`.");
        Field(x => x.Tip)
            .Description("The tip to include in the transaction.");
    }
}