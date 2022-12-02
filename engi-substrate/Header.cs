using System.Text.Json.Serialization;
using Blake2Core;

namespace Engi.Substrate;

public class Header : IEquatable<Header>
{
    [JsonPropertyName("parentHash")]
    public string ParentHash { get; init; } = null!;

    [JsonPropertyName("number"), JsonConverter(typeof(HexUInt64JsonConverter))]
    public ulong Number { get; init; }

    [JsonPropertyName("extrinsicsRoot")]
    public string ExtrinsicsRoot { get; init; } = null!;

    [JsonPropertyName("stateRoot")]
    public string StateRoot { get; init; } = null!;

    [JsonPropertyName("digest")]
    public Digest Digest { get; init; } = null!;

    [JsonIgnore] 
    public Lazy<string> Hash { get; init; }

    public Header()
    {
        Hash = new Lazy<string>(ComputeHash);
    }

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
    private string ComputeHash()
    {
        using var writer = new ScaleStreamWriter();

        writer.WriteHex0X(ParentHash);
        writer.WriteCompact(Number);
        writer.WriteHex0X(StateRoot);
        writer.WriteHex0X(ExtrinsicsRoot);
        writer.WriteCompact((ulong)Digest.Logs.Length);

        foreach (var log in Digest.Logs)
        {
            writer.WriteHex0X(log);
        }

        byte[] hashData = Blake2B.ComputeHash(writer.GetBytes(), new Blake2BConfig { OutputSizeInBits = 256 });

        return Hex.GetString0X(hashData);
    }
}