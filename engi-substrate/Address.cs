using Blake2Core;
using Engi.Substrate.Keys;
using Engi.Substrate.Metadata.V14;
using SimpleBase;

namespace Engi.Substrate;

public class Address : IScaleSerializable, IPublicKey, IEquatable<string>
{
    public string Id { get; }

    public byte[] Raw { get; }

    byte[] IPublicKey.PublicKey => Raw;

    public override string ToString() => Id;

    private Address(string id, byte[] raw)
    {
        Id = id;
        Raw = raw;
    }

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata _)
    {
        writer.Write(Raw);
    }

    public static Address From(byte[] raw) => new(Encode(raw), raw);

    public static Address Parse(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id));
        }

        return new(id, Decode(id));
    }

    public static bool TryParse(string id, out Address? address)
    {
        try
        {
            address = Parse(id);
            return true;
        }
        catch (ArgumentException)
        {
            address = null;
            return false;
        }
    }

    public static Address Parse(ScaleStreamReader reader)
    {
        byte[] raw = reader.ReadFixedSizeByteArray(32);

        return From(raw);
    }

    public static implicit operator Address(string s) => Parse(s);

    public static implicit operator string(Address address) => address.Id;

    public bool Equals(string? s)
    {
        return s != null && s == Id;
    }
    
    public override bool Equals(object? o)
    {
        return o switch
        {
            Address address => address.Id == Id,
            string s => s == Id,
            _ => false
        };
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    // helpers

    private static byte[] Decode(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentNullException(nameof(address));
        }

        Span<byte> decoded;
        
        try
        {
            decoded = Base58.Bitcoin.Decode(address);
        }
        catch (ArgumentException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(address), address, "Unable to parse address as base58.");
        }

        int ss58Length = (decoded[0] & 0b0100_0000) == 1 ? 2 : 1;

        // 32/33 bytes public + 2 bytes checksum + prefix
        bool isPublicKey = new[] { 34 + ss58Length, 35 + ss58Length }.Contains(decoded.Length);
        int length = decoded.Length - (isPublicKey ? 2 : 1);

        var result = decoded.Slice(ss58Length, length - ss58Length).ToArray();

        if (result.Length != 32)
        {
            throw new ArgumentException(
                "Decoded address is not 32 bytes long", nameof(address));
        }

        return result;
    }

    private static string Encode(Span<byte> bytes)
    {
        if (bytes.Length != 32)
        {
            throw new ArgumentException("Address is not 32 bytes long", nameof(bytes));
        }

        var SR25519_PUBLIC_SIZE = 32;
        var PUBLIC_KEY_LENGTH = 32;

        var plainAddr = Enumerable
            .Repeat((byte)0x2A, 35)
            .ToArray();

        bytes.CopyTo(plainAddr.AsSpan(1));

        var ssPrefixed = new byte[SR25519_PUBLIC_SIZE + 8];
        var ssPrefixed1 = new byte[] { 0x53, 0x53, 0x35, 0x38, 0x50, 0x52, 0x45 };
        ssPrefixed1.CopyTo(ssPrefixed, 0);
        plainAddr.AsSpan(0, SR25519_PUBLIC_SIZE + 1).CopyTo(ssPrefixed.AsSpan(7));

        var blake2bHashed = Blake2B.ComputeHash(ssPrefixed, 0, SR25519_PUBLIC_SIZE + 8);
        plainAddr[1 + PUBLIC_KEY_LENGTH] = blake2bHashed[0];
        plainAddr[2 + PUBLIC_KEY_LENGTH] = blake2bHashed[1];

        var addrCh = Base58.Bitcoin.Encode(plainAddr).ToArray();

        return new string(addrCh);
    }
}
