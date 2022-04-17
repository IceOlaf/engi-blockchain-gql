namespace Engi.Substrate;

public static class Address
{
    public static byte[] Decode(string address)
    {
        byte[] decoded = Base58Encoding.Decode(address);
        int ss58Length = (decoded[0] & 0b0100_0000) == 1 ? 2 : 1;

        // 32/33 bytes public + 2 bytes checksum + prefix
        bool isPublicKey = new[] { 34 + ss58Length, 35 + ss58Length }.Contains(decoded.Length);
        int length = decoded.Length - (isPublicKey ? 2 : 1);

        return decoded.AsSpan(ss58Length, length - ss58Length).ToArray();
    }
}