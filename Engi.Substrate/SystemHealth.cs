namespace Engi.Substrate;

public class SystemHealth
{
    public long Peers { get; set; }

    public bool IsSyncing { get; set; }

    public bool ShouldHavePeers { get; set; }
}