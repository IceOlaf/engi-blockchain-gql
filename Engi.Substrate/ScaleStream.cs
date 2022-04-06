using System.Numerics;
using System.Text;

namespace Engi.Substrate;

public class ScaleStream : Stream
{
    private readonly MemoryStream inner;

    public ScaleStream(byte[]? data)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data must not be null or empty", nameof(data));
        }

        inner = new MemoryStream(data, false);
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public bool HasNext => Position < Length;

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    [Obsolete($"Use {nameof(RequireByte)} because the semantics are different.")]
    public override int ReadByte() => throw new NotSupportedException();

    public bool? ReadOptionalBool()
    {
        return inner.ReadByte() switch
        {
            2 => true,
            1 => false,
            0 => null,
            _ => throw new InvalidDataException("Invalid bool? value")
        };
    }

    public bool ReadBool()
    {
        return inner.ReadByte() switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException("Invalid bool? value")
        };
    }

    public int ReadInt32()
    {
        byte[] data = new byte[4];
        
        if (inner.Read(data, 0, 4) < 4)
        {
            throw new InvalidDataException();
        }
        
        return BitConverter.ToInt32(data, 0);
    }

    public ushort ReadUInt16()
    {
        byte[] data = new byte[2];

        if (inner.Read(data, 0, 2) < 2)
        {
            throw new InvalidDataException();
        }

        return BitConverter.ToUInt16(data, 0);
    }

    public uint ReadUInt32()
    {
        byte[] data = new byte[4];

        if (inner.Read(data, 0, 4) < 4)
        {
            throw new InvalidDataException();
        }

        return BitConverter.ToUInt32(data, 0);
    }

    public ulong ReadUInt64()
    {
        byte[] data = new byte[8];

        if (inner.Read(data, 0, 8) < 8)
        {
            throw new InvalidDataException();
        }

        return BitConverter.ToUInt64(data, 0);
    }

    public BigInteger ReadUInt128()
    {
        byte[] data = new byte[16];

        if(inner.Read(data, 0, 16) < 16)
        {
            throw new InvalidDataException();
        }

        return new BigInteger(data);
    }

    public object ReadEnum(Type type)
    {
        object result = Enum.ToObject(type, RequireByte());

        if (!Enum.IsDefined(type, result))
        {
            throw new InvalidDataException();
        }

        return result;
    }

    public T ReadEnum<T>()
        where T : struct, Enum
    {
        T result = (T)Enum.ToObject(typeof(T), RequireByte());

        if (!Enum.IsDefined(result))
        {
            throw new InvalidDataException();
        }

        return result;
    }

    public ulong ReadCompactInteger()
    {
        int b0 = RequireByte();
        uint mode = (uint)b0 & 0x03;

        if (mode == 0)
        {
            return (ulong) b0 >> 2;
        }

        if (mode == 1)
        {
            return (ulong) (
                (b0 >> 2) + (RequireByte() << 6)
            );
        }

        if (mode == 2)
        {
            return (ulong)(
                  (b0 >> 2) 
                + (RequireByte() << 6)
                + (RequireByte() << 14)
                + (RequireByte() << 22)
            );
        }

        throw new NotSupportedException(
            $"Compact integers larger than four bytes must use {nameof(ReadCompactBigInteger)}");
    }

    public BigInteger ReadCompactBigInteger()
    {
        int b0 = RequireByte();
        uint mode = (uint)b0 & 0x03;

        if (mode == 0)
        {
            return new BigInteger((ulong)b0 >> 2);
        }

        if (mode == 1)
        {
            return new BigInteger((ulong)(
                (b0 >> 2) + (RequireByte() << 6)
            ));
        }

        if (mode == 2)
        {
            return new BigInteger((ulong)(
                (b0 >> 2)
                + (RequireByte() << 6)
                + (RequireByte() << 14)
                + (RequireByte() << 22)
            ));
        }

        int length = (b0 >> 2) + 4;

        byte[] data = new byte[length + 1];

        for(int i = 0; i < length; ++i)
        {
            data[i] = (byte) RequireByte();
        }

        return new BigInteger(data);
    }

    public string ReadString()
    {
        int length = (int) ReadCompactInteger();
        byte[] data = new byte[length];
        
        int read = inner.Read(data, 0, length);
        
        if (read < length)
        {
            throw new InvalidDataException();
        }

        return Encoding.UTF8.GetString(data);
    }

    public ushort[] ReadListOfUInt16()
    {
        int length = (int) ReadCompactInteger();
        
        ushort[] data = new ushort[length];
        byte[] buffer = new byte[2];
        
        for (int i = 0; i < length; ++i)
        {
            if (inner.Read(buffer, 0, 2) < 2)
            {
                throw new InvalidDataException();
            }

            data[i] = BitConverter.ToUInt16(buffer, 0);
        }

        return data;
    }

    public object? Read<T>()
    {
        Type t = typeof(T);

        if (t.IsPrimitive)
        {
            return ReadPrimitive(Type.GetTypeCode(t));
        }

        if (t.IsEnum)
        {
            return ReadEnum(t);
        }

        if (t == typeof(bool?))
        {
            return ReadOptionalBool();
        }

        if (t == typeof(BigInteger))
        {
            return ReadCompactBigInteger();
        }

        if (t == typeof(List<ushort>))
        {
            return ReadListOfUInt16();
        }

        throw new NotSupportedException(
            $"Reading of {typeof(T).FullName} is not supported");
    }

    public (object result, int index) ReadUnion<T1, T2>()
    {
        int index = RequireByte();

        object result = index switch
        {
            0 => Read<T1>()!,
            1 => Read<T2>()!,
            _ => throw new InvalidDataException()
        };

        return (result, index);
    }

    // helpers

    private int RequireByte()
    {
        int b = inner.ReadByte();

        if (b == -1)
        {
            throw new InvalidDataException();
        }

        return b;
    }

    private object ReadPrimitive(TypeCode typeCode)
    {
        return typeCode switch
        {
            TypeCode.Boolean => ReadBool(),
            TypeCode.Byte => (byte)RequireByte(),
            TypeCode.Int32 => ReadInt32(),
            TypeCode.UInt16 => ReadUInt16(),
            TypeCode.UInt32 => ReadUInt32(),
            TypeCode.UInt64 => ReadUInt64(),
            TypeCode.String => ReadString(),
            _ => throw new NotSupportedException($"TypeCode {typeCode} is not supported")
        };
    }
}