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
            .Description("Sort order: NEWEST_FIRST, OLDEST_FIRST, AMOUNT_ASCENDING, AMOUNT_DESCENDING");
    }
}
