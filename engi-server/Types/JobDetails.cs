using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class JobDetails
{
    public Job Job { get; set; } = null!;

    public UserInfo CreatorUserInfo { get; set; } = null!;
}
