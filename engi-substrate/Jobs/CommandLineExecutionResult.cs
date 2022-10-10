namespace Engi.Substrate.Jobs;

public class CommandLineExecutionResult
{
    public string Stdout { get; set; } = null!;

    public string Stderr { get; set; } = null!;

    public int ReturnCode { get; set; }
}