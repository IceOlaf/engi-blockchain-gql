namespace Engi.Substrate.Server.Async;

public class ConsistencyCheckCommand
{
    public string Id { get; set; } = Key;

    public DateTime? LastExecutedOn { get; set; }

    public long? LastRecovered { get; set; }

    public const string Key = nameof(ConsistencyCheckCommand);
}
