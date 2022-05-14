namespace Engi.Substrate;

public class ScaleStreamWriter : IDisposable
{
    private readonly Stream stream;

    public ScaleStreamWriter(Stream stream)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public void Write(byte b) => stream.WriteByte(b);

    public void Write(ReadOnlySpan<byte> b) => stream.Write(b);

    public void Write(uint value) => stream.Write(BitConverter.GetBytes(value));

    public void WriteCompact(ulong value)
    {
        var result = Compact(value);

        stream.Write(result);
    }

    public void WriteHex0x(string s)
    {
        byte[] bytes = Hex.GetBytes0x(s);

        stream.Write(bytes);
    }

    public void Dispose()
    {
        stream.Dispose();
    }

    // statics

    public static int GetCompactLength(ulong value)
    {
        return value switch
        {
            <= 0x3f => 1,
            <= 0x3ff => 2,
            <= 0x3fffffff => 4,
            _ => throw new InvalidDataException()
        };
    }

    public static byte[] Compact(ulong value)
    {
        int count = GetCompactLength(value);

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