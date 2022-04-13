namespace Engi.Substrate.Server.Schemas;

public class EngiHealth
{
    public string? Chain { get; set; }
    public string? NodeName { get; set; }
    public string? Version { get; set; }
    public EngiHealthStatus Status { get; set; }
}