namespace Engi.Substrate.Server.Types;

public class UserInfo
{
    public string Address { get; set; } = null!;

    public int CreatedJobsCount { get; set; }

    public int SolvedJobsCount { get; set; }
}
