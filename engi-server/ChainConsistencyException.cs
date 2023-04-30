namespace Engi.Substrate.Server;

/// <summary>
/// This exception is thrown when an assumption we make about
/// data from the chain is not respected.
/// </summary>
public class ChainAssumptionInconsistencyException : Exception
{
    public ChainAssumptionInconsistencyException(string message)
        : base(message)
    {
    }
}
