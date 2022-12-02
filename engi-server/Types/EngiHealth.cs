namespace Engi.Substrate.Server.Types;

public class EngiHealth
{
    public string? Chain { get; set; }
    public string? NodeName { get; set; }
    public string? Version { get; set; }
    public EngiHealthStatus Status { get; set; }
    public long? PeerCount { get; set; }
}