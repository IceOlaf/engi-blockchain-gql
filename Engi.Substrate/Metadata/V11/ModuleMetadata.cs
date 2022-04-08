namespace Engi.Substrate.Metadata.V11;

public class ModuleMetadata
{
    public string? Name { get; set; }

    public StorageMetadata? Storage { get; set; }

    public FunctionMetadata[] Calls { get; set; } = Array.Empty<FunctionMetadata>();
    public EventMetadata[] Events { get; set; } = Array.Empty<EventMetadata>();
    public ModuleConstantMetadata[]? Constants { get; set; }
    public ErrorMetadata[]? Errors { get; set; }
}