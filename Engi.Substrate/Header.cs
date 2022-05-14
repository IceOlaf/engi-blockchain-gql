using System.Text.Json.Serialization;
using Blake2Core;

namespace Engi.Substrate;

public class Header : IEquatable<Header>
{
    [JsonPropertyName("parentHash")]
    public string ParentHash { get; set; } = null!;

    [JsonPropertyName("number"), JsonConverter(typeof(HexUInt64JsonConverter))]
    public ulong Number { get; set; }

    [JsonPropertyName("extrinsicsRoot")]
    public string ExtrinsicsRoot { get; set; } = null!;

    [JsonPropertyName("stateRoot")]
    public string StateRoot { get; set; } = null!;

    [JsonPropertyName("digest")]
    public Digest Digest { get; set; } = null!;

    public bool Equals(Header? other)
    {
        if (other == null)
        {
            return false;
        }

        return ComputeHash()
            .SequenceEqual(other.ComputeHash());
    }

    // TODO: Lazy it
    public byte[] ComputeHash()
    {
        using var ms = new MemoryStream();
        using var writer = new ScaleStreamWriter(ms);

        writer.WriteHex0x(ParentHash);
        writer.WriteCompact(Number);
        writer.WriteHex0x(StateRoot);
        writer.WriteHex0x(ExtrinsicsRoot);
        writer.WriteCompact((ulong)Digest.Logs.Length);
        foreach (var log in Digest.Logs)
        {
            writer.WriteHex0x(log);
        }

        return Blake2B.ComputeHash(ms.ToArray(), new Blake2BConfig { OutputSizeInBits = 256 });
    }
}