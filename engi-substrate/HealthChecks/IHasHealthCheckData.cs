namespace Engi.Substrate.HealthChecks;

public interface IHasHealthCheckData
{
    IReadOnlyDictionary<string, object?> GetHealthCheckData();
}
