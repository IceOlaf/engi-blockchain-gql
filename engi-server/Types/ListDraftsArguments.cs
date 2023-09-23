using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Types;

public class ListDraftsArguments
{
    public int Skip { get; set; }

    public int Take { get; set; }
}
