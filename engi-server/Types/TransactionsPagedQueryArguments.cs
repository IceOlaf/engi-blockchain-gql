using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Server.Indexing;

namespace Engi.Substrate.Server.Types;

public class TransactionsPagedQueryArguments : PagedQueryArguments
{
    [Required]
    public string AccountId { get; set; } = null!;

    public TransactionType? Type { get; set; }
}