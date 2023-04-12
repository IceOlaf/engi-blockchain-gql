using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class JobsQueryStaticData
{
    public Technology[] Technologies => Enum.GetValues<Technology>();
}
