using Engi.Substrate.Server.Indexing;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class TransactionGraphType : ObjectGraphType<TransactionIndex.Result>
{
    public TransactionGraphType()
    {
        Field(x => x.Number);
        Field(x => x.Hash);
        Field(x => x.DateTime);
        Field(x => x.Type);
        Field(x => x.IsSuccessful);
        Field(x => x.OtherParticipants, nullable: true);
        Field(x => x.Amount);
        Field(x => x.JobId);
    }
}