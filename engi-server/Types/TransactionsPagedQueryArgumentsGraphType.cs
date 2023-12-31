namespace Engi.Substrate.Server.Types;

public class TransactionsPagedQueryArgumentsGraphType : PagedQueryArgumentsGraphType<TransactionsPagedQueryArguments>
{
    public TransactionsPagedQueryArgumentsGraphType()
    {
        Description = "The paged query arguments for the transactions query.";

        Field(x => x.AccountId)
            .Description("The account id to query for.");
        Field(x => x.Type, nullable: true)
            .Description("The type of transaction to filter for.");
        Field(x => x.SortBy, nullable: true)
            .Description("Sort order: CREATED_ASCENDING, CREATED_DESCENDING, AMOUNT_ASCENDING, AMOUNT_DESCENDING");
    }
}
