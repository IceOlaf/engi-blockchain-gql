using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Indexing;
using Engi.Substrate.Server.Types.Validation;

namespace Engi.Substrate.Server.Types;

public class TransactionsPagedQueryArguments : PagedQueryArguments
{
    [Required, AccountId]
    public string AccountId { get; set; } = null!;

    public TransactionType? Type { get; set; }

    public TransactionSortOrder? SortBy { get; set; }
}
