namespace Engi.Substrate.Server.Async;

public class ConsistencyCheckCommand
{
    public string Id { get; set; } = Key;

    public DateTime? StartedOn { get; set; }

    public bool IsRunning => StartedOn.HasValue;

    public DateTime? LastCompletedOn { get; set; }

    public TimeSpan LastCompletedDuration { get; set; }

    public ulong[] LastRecoveredBlockNumbers { get; set; } = null!;


    public const string Key = nameof(ConsistencyCheckCommand);
}
