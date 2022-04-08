namespace Engi.Substrate.Metadata.V11;

public class FunctionMetadata
{
    public string? Name { get; set; }
    public FunctionArgumentMetadata[]? Arguments { get; set; }
    public string[]? Documentation { get; set; }
}