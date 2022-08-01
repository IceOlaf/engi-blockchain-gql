using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class ChainState
{
    public RuntimeMetadata Metadata { get; set; } = null!;

    public RuntimeVersion Version { get; set; } = null!;

    public string GenesisHash { get; set; } = null!;

    public Header LatestFinalizedHeader { get; set; } = null!;
}