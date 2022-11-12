using System.Text.Json;

namespace Engi.Substrate;

public class BlockHeaderNotFoundException : SubstrateException
{
    public string Hash { get; }

    public BlockHeaderNotFoundException(string hash, string code, string message, JsonElement? data)
        : base(code, message, data)
    {
        Hash = hash;
    }
}
