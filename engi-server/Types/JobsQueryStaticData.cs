using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class JobsQueryStaticData
{
    public Language[] Languages => Enum.GetValues<Language>();
}
