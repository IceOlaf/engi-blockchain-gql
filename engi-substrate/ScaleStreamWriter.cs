using System.Numerics;
using System.Text;

namespace Engi.Substrate;

public class ScaleStreamWriter : IDisposable
{
    private readonly Stream stream;
    private readonly bool keepOpen;

    public ScaleStreamWriter(Stream stream, bool keepOpen = false)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.keepOpen = keepOpen;
    }

    public ScaleStreamWriter()
        : this(new MemoryStream())
    { }

    public void Write(byte b) => stream.WriteByte(b);

    public void Write(ReadOnlySpan<byte> b) => stream.Write(b);

    public void Write(int value) => stream.Write(BitConverter.GetBytes(value));

    public void Write(uint value) => stream.Write(BitConverter.GetBytes(value));

    public void Write(ulong value) => stream.Write(BitConverter.GetBytes(value));

    public void WriteU128(BigInteger value)
    {
        var bytes = value.ToByteArray(isUnsigned: true);

        if (bytes.Length < 16)
        {
            int diffCount = 16 - bytes.Length;

            Write(new byte[diffCount]);
        }

        Write(bytes);
    }

    public void Write(string s)
    {
        WriteCompact(s.Length);
        Write(Encoding.UTF8.GetBytes(s));
    }

    public void Write<T>(T e)
        where T : Enum
    {
        Write(Convert.ToByte(e));
    }

    public void WriteCompact(ulong value)
    {
        var result = Compact(value);

        stream.Write(result);
    }

    public void WriteCompact(BigInteger value)
    {
        int count = GetCompactLength(value);

        if (count != -1)
        {
            WriteCompact((ulong) value);
            return;
        }

        byte[] data = value.ToByteArray();
        int length = data.Length - CountLastZeros(data);

        Write((byte)(((length - 4) << 2) + 0b11));

        for(int i = 0; i < length; ++i)
        {
            Write(data[i]);
        }
    }

    public void WriteHex0X(string s)
    {
        byte[] bytes = Hex.GetBytes0X(s);

        stream.Write(bytes);
    }

    public void Write(IScaleSerializable serializable)
    {
        serializable.Serialize(this);
    }

    public void Write<T>(IList<T> items, Action<ScaleStreamWriter, T> writeItemFunc)
    {
        WriteCompact(items.Count);

        foreach (var item in items)
        {
            writeItemFunc(this, item);
        }
    }

    public void Write(string[] items)
    {
        WriteCompact(items.Length);

        foreach (var item in items)
        {
            Write(item);
        }
    }

    public void Write(IScaleSerializable[] items)
    {
        WriteCompact(items.Length);

        foreach (var item in items)
        {
            Write(item);
        }
    }

    public byte[] GetBytes()
    {
        if (stream is MemoryStream ms)
        {
            return ms.ToArray();
        }

        throw new NotSupportedException(
            $"{nameof(GetBytes)} is only supported when created with the default ctor or when passing in a MemoryStream.");
    }

    public void Dispose()
    {
        if (!keepOpen)
        {
            stream.Dispose();
        }
    }

    // statics

    public static int GetCompactLength(ulong value)
    {
        return value switch
        {
            <= 0x3f => 1,
            <= 0x3fff => 2,
            <= 0x3fffffff => 4,
            _ => -1
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

    public static int GetCompactLength(BigInteger value)
    {
        if (value <= 0x3f)
        {
            return 1;
        }

        if (value <= 0x3fff)
        {
            return 2;
        }

        if (value <= 0x3fffffff)
        {
            return 4;
        }

        return -1;
    }

    // helpers

    private static int CountLastZeros(byte[] b)
    {
        int count = 0;
        int i = b.Length - 1;

        while (b[i] == 0 && i-- != 0)
        {
            count++;
        }

        return count;
    }
}