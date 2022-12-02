namespace Engi.Substrate.Metadata.V14;

public class RuntimeVersion
{
    public string SpecName { get; set; } = null!;

    public string ImplName { get; set; } = null!;
    
    public uint AuthoringVersion { get; set; }

    public uint SpecVersion { get; set; }
    
    public uint ImplVersion { get; set; }
    
    public object[][] Apis { get; set; } = null!;

    public uint TransactionVersion { get; set; }
}