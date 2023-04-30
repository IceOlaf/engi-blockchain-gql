using Engi.Substrate.Indexing;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class TransactionGraphType : ObjectGraphType<TransactionIndex.Result>
{
    public TransactionGraphType()
    {
        Description = "A transaction recorded related to an ENGI user.";

        Field(x => x.Number)
            .Description("The number of the block that contains this transaction.");
        Field(x => x.Hash)
            .Description("The hash of the block that contains this transaction.");
        Field(x => x.DateTime)
            .Description("The date time encode in the block from the Timestamp::now pallet call.");
        Field(x => x.Type)
            .Description("The transaction type.");
        Field(x => x.Executor)
            .Description("The address of the executor.");
        Field(x => x.IsSuccessful)
            .Description("A boolean indicating whether the transaction was successful. If the transaction required sudo privileges, it equates to the sudo success and not the extrinsic success.");
        Field(x => x.OtherParticipants, nullable: true)
            .Description("The addresses that participated in this transaction, other than the executor, if any.");
        Field(x => x.Amount)
            .Description("The amount associated with this transaction.");
        Field(x => x.JobId, nullable: true)
            .Description("The job ID associated with this transaction, if any.");
    }
}
