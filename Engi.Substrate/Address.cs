using SimpleBase;

namespace Engi.Substrate;

public class Address
{
    public string Id { get; set; }

    public byte[] Raw { get; set; }

    private Address(string id, byte[] raw)
    {
        Id = id;
        Raw = raw;
    }

    public static Address From(string id) => new(id, Decode(id));

    public static Address From(byte[] raw) => new(Encode(raw), raw);

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
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Invalid address", nameof(address), ex);
        }

        int ss58Length = (decoded[0] & 0b0100_0000) == 1 ? 2 : 1;

        // 32/33 bytes public + 2 bytes checksum + prefix
        bool isPublicKey = new[] { 34 + ss58Length, 35 + ss58Length }.Contains(decoded.Length);
        int length = decoded.Length - (isPublicKey ? 2 : 1);

        var result = decoded.Slice(ss58Length, length - ss58Length).ToArray();

        if (result.Length != 32)
        {
            throw new InvalidOperationException(
                "Decoded address is not 32 bytes long");
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

        var blake2bHashed = Hashing.Blake2(ssPrefixed, 0, SR25519_PUBLIC_SIZE + 8);
        plainAddr[1 + PUBLIC_KEY_LENGTH] = blake2bHashed[0];
        plainAddr[2 + PUBLIC_KEY_LENGTH] = blake2bHashed[1];

        var addrCh = Base58.Bitcoin.Encode(plainAddr).ToArray();

        return new string(addrCh);
    }
}