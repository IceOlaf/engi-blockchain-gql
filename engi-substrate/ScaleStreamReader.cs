using System.Diagnostics;
using System.Numerics;
using System.Text;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

[DebuggerDisplay("{Remainder}")]
public class ScaleStreamReader : IDisposable
{
    private readonly MemoryStream inner;

    public ScaleStreamReader(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data must not be null or empty", nameof(data));
        }

        inner = new MemoryStream(data, false);
    }

    public ScaleStreamReader(string hexString)
    {
        if (hexString == null)
        {
            throw new ArgumentNullException(nameof(hexString));
        }

        if (!hexString.StartsWith("0x"))
        {
            throw new ArgumentException("Hex string is assumed to start with 0x", nameof(hexString));
        }

        var data = Hex.GetBytes(hexString.AsSpan(2));

        inner = new MemoryStream(data, false);
    }

    public long Position => inner.Position;

    public bool HasNext => inner.Position < inner.Length;

    internal string Remainder
    {
        get
        {
            if (!HasNext)
            {
                return "EOF";
            }

            return Hex.GetString0X(inner.ToArray().Skip((int) inner.Position).ToArray());
        }
    }

    public void Dispose() => inner.Dispose();

    public int ReadByte()
    {
        int b = inner.ReadByte();

        if (b == -1)
        {
            throw new InvalidDataException();
        }

        return b;
    }

    public bool? ReadOptionalBool()
    {
        int value = inner.ReadByte();
        return value switch
        {
            2 => true,
            1 => false,
            0 => null,
            _ => throw new InvalidDataException("Invalid bool? value")
        };
    }

    public bool ReadBool()
    {
        int value = inner.ReadByte();
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException("Invalid bool value")
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
        object result = Enum.ToObject(type, ReadByte());

        if (!Enum.IsDefined(type, result))
        {
            throw new InvalidDataException();
        }

        return result;
    }

    public T ReadEnum<T>()
        where T : struct, Enum
    {
        T result = (T)Enum.ToObject(typeof(T), ReadByte());

        if (!Enum.IsDefined(result))
        {
            throw new InvalidDataException();
        }

        return result;
    }

    public ulong ReadCompactInteger()
    {
        int b0 = ReadByte();
        uint mode = (uint)b0 & 0x03;

        if (mode == 0)
        {
            return (ulong) b0 >> 2;
        }

        if (mode == 1)
        {
            return (ulong) (
                (b0 >> 2) + (ReadByte() << 6)
            );
        }

        if (mode == 2)
        {
            return (ulong)(
                  (b0 >> 2) 
                + (ReadByte() << 6)
                + (ReadByte() << 14)
                + (ReadByte() << 22)
            );
        }

        throw new NotSupportedException(
            $"Compact integers larger than four bytes must use {nameof(ReadCompactBigInteger)}");
    }

    public BigInteger ReadCompactBigInteger()
    {
        int b0 = ReadByte();
        uint mode = (uint)b0 & 0x03;

        if (mode == 0)
        {
            return new BigInteger((ulong)b0 >> 2);
        }

        if (mode == 1)
        {
            return new BigInteger((ulong)(
                (b0 >> 2) + (ReadByte() << 6)
            ));
        }

        if (mode == 2)
        {
            return new BigInteger((ulong)(
                (b0 >> 2)
                + (ReadByte() << 6)
                + (ReadByte() << 14)
                + (ReadByte() << 22)
            ));
        }

        int length = (b0 >> 2) + 4;

        byte[] data = new byte[length + 1];

        for(int i = 0; i < length; ++i)
        {
            data[i] = (byte) ReadByte();
        }

        return new BigInteger(data);
    }

    public string? ReadString(bool returnNullIfEmpty = true)
    {
        int length = (int) ReadCompactInteger();

        if (length == 0 && returnNullIfEmpty)
        {
            return null;
        }

        byte[] data = new byte[length];
        
        int read = inner.Read(data, 0, length);
        
        if (read < length)
        {
            throw new InvalidDataException();
        }

        return Encoding.UTF8.GetString(data);
    }

    public byte[] ReadFixedSizeByteArray(uint length)
    {
        byte[] data = new byte[length];

        if (inner.Read(data, 0, (int)length) < length)
        {
            throw new InvalidDataException();
        }

        return data;
    }

    public byte[] ReadByteArray()
    {
        ulong length = ReadCompactInteger();

        return ReadFixedSizeByteArray((uint)length);
    }

    public T[] ReadList<T>(Func<ScaleStreamReader, T> func)
    {
        ulong length = ReadCompactInteger();

        return ReadList(length, func);
    }

    public T[] ReadList<T>(ulong length, Func<ScaleStreamReader, T> func)
    {
        if (length == 0)
        {
            return Array.Empty<T>();
        }

        T[] data = new T[length];

        for (ulong i = 0; i < length; ++i)
        {
            data[i] = func(this);
        }

        return data;
    }

    public object? Read(Type t)
    {
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

        if (t.IsGenericType)
        {
            var genericTypeDefinition = t.GetGenericTypeDefinition();

            if (genericTypeDefinition != typeof(List<>))
            {
                throw new NotSupportedException("Only List<T> is supported right now");
            }

            var innerType = t.GetGenericArguments().First();

            if (IsValid(innerType))
            {
                throw new InvalidOperationException(
                    $"Cannot deserialize type={innerType}");
            }

            return ReadList(stream => stream.Read(innerType));
        }

        throw new NotSupportedException(
            $"Reading of {t.FullName} is not supported");
    }

    public object ReadPrimitive(PrimitiveType type)
    {
        return type switch
        {
            PrimitiveType.Bool => ReadBool(),
            PrimitiveType.Char => (char)ReadByte(),
            PrimitiveType.Int32 => ReadInt32(),
            PrimitiveType.Int8 => (sbyte)ReadByte(),
            PrimitiveType.String => ReadString(returnNullIfEmpty: false)!,
            PrimitiveType.UInt8 => (byte)ReadByte(),
            PrimitiveType.UInt16 => ReadUInt16(),
            PrimitiveType.UInt32 => ReadUInt32(),
            PrimitiveType.UInt64 => ReadUInt64(),
            PrimitiveType.UInt128 => ReadUInt128(),
            _ => throw new NotImplementedException(
                $"Reading of primitive type '{type}' is not implemented.")
        };
    }

    public T? Read<T>()
    {
        Type t = typeof(T);

        return (T?) Read(t);
    }

    public T? ReadOptional<T>(Func<ScaleStreamReader, T> func)
    {
        return ReadBool() ? func(this) : default;
    }

    public (object result, int index) ReadUnion<T1, T2>()
    {
        int index = ReadByte();

        object result = index switch
        {
            0 => Read<T1>()!,
            1 => Read<T2>()!,
            _ => throw new InvalidDataException()
        };

        return (result, index);
    }

    public string ReadAddressAsId()
    {
        var bytes = ReadFixedSizeByteArray(32);

        var address = Address.From(bytes);

        return address.Id;
    }

    // helpers

    private object? ReadPrimitive(TypeCode typeCode)
    {
        return typeCode switch
        {
            TypeCode.Boolean => ReadBool(),
            TypeCode.Byte => (byte)ReadByte(),
            TypeCode.Int32 => ReadInt32(),
            TypeCode.UInt16 => ReadUInt16(),
            TypeCode.UInt32 => ReadUInt32(),
            TypeCode.UInt64 => ReadUInt64(),
            TypeCode.String => ReadString(),
            _ => throw new NotSupportedException($"TypeCode {typeCode} is not supported")
        };
    }

    private bool IsValid(Type t)
    {
        return t.IsPrimitive
               || t.IsEnum
               || t == typeof(bool?)
               || t == typeof(BigInteger);
    }
}