using Engi.Substrate.Identity;

namespace Engi.Substrate.Server.Email;

public class EmailModel
{
    public ApplicationOptions Application { get; init; } = null!;

    public User User { get; init; } = null!;

    public Dictionary<string, object> Data { get; init; } = null!;
}