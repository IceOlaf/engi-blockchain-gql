using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class ChainSnapshot
{
    public RuntimeMetadata Metadata { get; set; } = null!;

    public string GenesisHash { get; set; } = null!;

    public string FinalizedBlockHash { get; set; } = null!;

    public Header LatestHeader { get; set; } = null!;

    public RuntimeVersion RuntimeVersion { get; set; } = null!;
}