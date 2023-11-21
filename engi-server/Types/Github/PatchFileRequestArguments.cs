using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class PatchFileRequestArguments
{
    public string BaseRepositoryUrl { get; set; } = null!;

    public string BaseRepositoryCommit { get; set; } = null!;

    public string ForkRepositoryUrl { get; set; } = null!;

    public string ForkRepositoryCommit { get; set; } = null!;
}
