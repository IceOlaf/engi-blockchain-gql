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

        ms.Write(Hex.GetBytes0x(ParentHash));
        ms.Write(Compact(Number));
        ms.Write(Hex.GetBytes0x(StateRoot));
        ms.Write(Hex.GetBytes0x(ExtrinsicsRoot));
        ms.Write(Compact((ulong)Digest.Logs.Length));
        foreach (var log in Digest.Logs)
        {
            ms.Write(Hex.GetBytes0x(log));
        }

        return Blake2B.ComputeHash(ms.ToArray(), new Blake2BConfig { OutputSizeInBits = 256 });
    }

	// TODO: beginnings of a ScaleStreamWriter
	private static byte[] Compact(ulong value)
    {
        int count = value switch
        {
            <= 0x3f => 1,
            <= 0x3ff => 2,
            <= 0x3fffffff => 4,
            _ => throw new InvalidDataException()
        };

        byte[] result = new byte[count];

        uint mode = count switch
        {
            2 => 0b01,
            4 => 0b10,
            _ => 0
        };

        ulong compact = (value << 2) + mode;

        for (int i = 0; i < count; ++i)
        {
            result[i] = (byte)compact;
            compact >>= 8;
        }

        return result;
    }
}