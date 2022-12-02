using System.Text;
using Blake2Core;
using Extensions.Data;

namespace Engi.Substrate;

public static class Hashing
{
    public static byte[] Blake2Concat(byte[] bytes, int size = 128)
    {
        var config = new Blake2BConfig { OutputSizeInBits = size, Key = null };
        return Blake2B.ComputeHash(bytes, config).Concat(bytes).ToArray();
    }

    public static byte[] Twox128(byte[] bytes)
    {
        ulong hash1 = XXHash.XXH64(bytes, 0);
        ulong hash2 = XXHash.XXH64(bytes, 1);

        byte[] result = new byte[2 * sizeof(ulong)];

        if (!BitConverter.TryWriteBytes(result, hash1))
        {
            throw new InvalidDataException();
        }

        if (!BitConverter.TryWriteBytes(result.AsSpan(sizeof(ulong)), hash2))
        {
            throw new InvalidDataException();
        }

        return result;
    }

    public static byte[] Twox128(string s)
    {
        return Twox128(Encoding.UTF8.GetBytes(s));
    }
}