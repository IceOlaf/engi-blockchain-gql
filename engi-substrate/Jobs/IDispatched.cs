namespace Engi.Substrate.Jobs;

public interface IDispatched
{
    DateTime? DispatchedOn { get; set; }
}
