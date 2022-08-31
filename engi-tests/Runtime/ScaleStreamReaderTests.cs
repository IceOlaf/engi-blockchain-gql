using System;
using System.IO;
using Xunit;

namespace Engi.Substrate;

public class ScaleStreamReaderTests
{
    private ScaleStreamReader FromHex(string hex)
    {
        return new ScaleStreamReader(Convert.FromHexString(hex));
    }

    [Fact]
    public void Ctor_ErrorsIfNoInput()
    {
        Assert.Throws<ArgumentException>(
            () => new ScaleStreamReader(Array.Empty<byte>()));
    }

    [Fact]
    public void OptionalBool_ReadsExistingTrueBool()
    {
        var stream = FromHex("02");

        Assert.Equal(true, stream.ReadOptionalBool());
        Assert.False(stream.HasNext);
    }

    [Fact]
    public void OptionalBool_ReadsExistingFalseBool()
    {
        var stream = FromHex("01");

        Assert.Equal(false, stream.ReadOptionalBool());
        Assert.False(stream.HasNext);
    }

    [Fact]
    public void OptionalBool_ReadsNoBool()
    {
        var stream = FromHex("00");

        Assert.Null(stream.ReadOptionalBool());
        Assert.False(stream.HasNext);
    }

    [Fact]
    public void OptionalBool_ErrorsIfInvalidValue()
    {
        var stream = FromHex("03");

        Assert.Throws<InvalidDataException>(
            () => stream.ReadOptionalBool());
    }

    [Fact]
    public void Bool_ReadsTrueBool()
    {
        var stream = FromHex("01");

        Assert.True(stream.ReadBool());
        Assert.False(stream.HasNext);
    }

    [Fact]
    public void Bool_ReadsFalseBool()
    {
        var stream = FromHex("00");

        Assert.False(stream.ReadBool());
        Assert.False(stream.HasNext);
    }

    [Fact]
    public void Bool_ErrorIfInvalidValue()
    {
        var stream = FromHex("02");

        Assert.Throws<InvalidDataException>(
            () => stream.ReadBool());
    }

    [Theory]
    [InlineData("00000000", 0)]
    [InlineData("01000000", 0x01)]
    [InlineData("01020304", 0x04030201)]
    [InlineData("ff000000", 0xff)]
    [InlineData("ffff0000", 0xffff)]
    [InlineData("ffffff00", 0xffffff)]
    [InlineData("ffffff7f", int.MaxValue)]
    public void Int32_Positive(string hex, int expected)
    {
        ScaleStreamReader stream = new(Convert.FromHexString(hex));

        Assert.Equal(expected, stream.ReadInt32());
    }

    [Theory]
    [InlineData("ffffffff", -1)]
    [InlineData("9cffffff", -100)]
    [InlineData("0100ffff", -0xffff)]
    [InlineData("fefeffff", -0x0102)]
    [InlineData("fdfdfeff", -0x010203)]
    [InlineData("00000080", int.MinValue)]
    public void Int32_Negative(string hex, int expected)
    {
        ScaleStreamReader stream = new(Convert.FromHexString(hex));

        Assert.Equal(expected, stream.ReadInt32());
    }

    [Fact]
    public void Int32_ErrorsForShortData()
    {
        var stream = FromHex("ff");

        Assert.Throws<InvalidDataException>(
            () => stream.ReadInt32());
    }

    [Fact]
    public void UInt16_Reads()
    {
        var stream = FromHex("2a00");

        Assert.Equal(42u, stream.ReadUInt16());
    }

    [Fact]
    public void UInt16_ErrorsForShortData()
    {
        var stream = FromHex("ff");

        Assert.Throws<InvalidDataException>(
            () => stream.ReadUInt16());
    }

    [Theory]
    [InlineData("0000", 0x00_00u)]
    [InlineData("00ff", 0xff_00u)]
    [InlineData("ff00", 0x00_ffu)]
    [InlineData("ffff", 0xff_ffu)]
    [InlineData("f0f0", 0xf0_f0u)]
    [InlineData("0f0f", 0x0f_0fu)]
    [InlineData("f00f", 0x0f_f0u)]
    public void UInt16_Read(string hex, ushort expected)
    {
        ScaleStreamReader stream = new(Convert.FromHexString(hex));

        Assert.Equal(expected, stream.ReadUInt16());
    }

    [Fact]
    public void UInt32_Reads()
    {
        var stream = FromHex("ffffff00");

        Assert.Equal(16777215U, stream.ReadUInt32());
    }

    [Fact]
    public void UInt32_ErrorsForShortData()
    {
        var stream = FromHex("ffffff");

        Assert.Throws<InvalidDataException>(
            () => stream.ReadUInt32());
    }

    [Theory]
    [InlineData("00000000", 0x00_00_00_00u)]
    [InlineData("000000ff", 0xff_00_00_00u)]
    [InlineData("0000ff00", 0x00_ff_00_00u)]
    [InlineData("00ff0000", 0x00_00_ff_00u)]
    [InlineData("ff000000", 0x00_00_00_ffu)]
    [InlineData("0f0f0f0f", 0x0f_0f_0f_0fu)]
    [InlineData("f0f0f0f0", 0xf0_f0_f0_f0u)]
    [InlineData("ffffff00", 0x00_ff_ff_ffu)]
    [InlineData("00060000", 0x00_00_06_00u)]
    [InlineData("00030000", 0x00_00_03_00u)]
    [InlineData("7d010000", 0x00_00_01_7du)]
    public void UInt32_ReadsAllCases(string hex, uint expected)
    {
        ScaleStreamReader stream = new(Convert.FromHexString(hex));

        Assert.Equal(expected, stream.ReadUInt32());
    }

    [Fact]
    public void UInt64_Reads()
    {
        var stream = FromHex("f70af5f6f3c84305");

        Assert.Equal(379367743775116023ul, stream.ReadUInt64());
    }

    [Fact]
    public void UInt64_ReadsWithZeroPrefix()
    {
        var stream = FromHex("0000c52ebca2b10000");

        Assert.Equal(50000000000000000ul, stream.ReadUInt64());
    }

    [Fact]
    public void UInt128_Reads()
    {
        var stream = FromHex("f70af5f6f3c843050000000000000000");

        Assert.Equal("379367743775116023", stream.ReadUInt128().ToString());
    }

    [Fact]
    public void UInt128_ReadsWithZeroPrefix()
    {
        var stream = FromHex("0000c52ebca2b1000000000000000000");

        Assert.Equal("50000000000000000", stream.ReadUInt128().ToString());
    }

    [Fact]
    public void UInt128_ErrorsForShortData()
    {
        var stream = FromHex("f70af5f6f3c84305000000000000");

        Assert.Throws<InvalidDataException>(
            () => stream.ReadUInt128());
    }

    enum TestEnum
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3
    }

    [Fact]
    public void Enum_ReadValues()
    {
        var stream = FromHex("0001020302");

        var expected = new[]
        {
            TestEnum.Zero,
            TestEnum.One,
            TestEnum.Two,
            TestEnum.Three,
            TestEnum.Two
        };

        var actual = new[]
        {
            stream.ReadEnum<TestEnum>(),
            stream.ReadEnum<TestEnum>(),
            stream.ReadEnum<TestEnum>(),
            stream.ReadEnum<TestEnum>(),
            stream.ReadEnum<TestEnum>()
        };

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Enum_ErrorsWithUnknownEnumValue()
    {
        var stream = FromHex("04");

        Assert.Throws<InvalidDataException>(
            () => stream.ReadEnum<TestEnum>());
    }

    [Theory]
    [InlineData("00", 0)]
    [InlineData("04", 1)]
    [InlineData("a8", 42)]
    [InlineData("fc", 63)]
    [InlineData("0101", 64)]
    [InlineData("1501", 69)]
    [InlineData("fdff", 16383)]
    [InlineData("02000100", 16384)]
    [InlineData("feffffff", 0x3f_ff_ff_fful)]
    public void CompactInteger_Reads(string hex, ulong expected)
    {
        var stream = FromHex(hex);

        Assert.Equal(expected, stream.ReadCompactInteger());
    }

    [Fact]
    public void CompactInteger_ThrowsIfBigInteger()
    {
        var stream = FromHex("33aabbccddeeff00112233445566778899");

        Assert.Throws<NotSupportedException>(
            () => stream.ReadCompactInteger());
    }

    [Theory]
    [InlineData("00", "0")]
    [InlineData("04", "1")]
    [InlineData("a8", "42")]
    [InlineData("fc", "63")]
    [InlineData("0101", "64")]
    [InlineData("1501", "69")]
    [InlineData("fdff", "16383")]
    [InlineData("02000100", "16384")]
    [InlineData("feffffff", "1073741823")]
    [InlineData("0300000040", "1073741824")]
    [InlineData("0370605040", "1079009392")]
    [InlineData("03000000ff", "4278190080")]
    [InlineData("030000ffff", "4294901760")]
    [InlineData("03ffffffff", "4294967295")]
    [InlineData("0700ffffffff", "1099511627520")]
    [InlineData("07ffffffffff", "1099511627775")]
    [InlineData("33aabbccddeeff00112233445566778899", "204080457442256950375158822971602942890")]
    public void CompactBigInteger_Reads(string hex, string expected)
    {
        var stream = FromHex(hex);

        Assert.Equal(expected, stream.ReadCompactBigInteger().ToString());
    }

    [Fact]
    public void String_Reads()
    {
        var stream = FromHex("3048656c6c6f20576f726c6421");

        Assert.Equal("Hello World!", stream.ReadString());
    }

    [Fact]
    public void List_ReadsUInt16()
    {
        var stream = FromHex("18040008000f00100017002a00");

        Assert.Equal(new ushort[]
        {
            4, 8, 15, 16, 23, 42
        }, stream.ReadList(stream => stream.ReadUInt16()));
    }

    [Theory]
    [InlineData("002a", 0, typeof(byte), (byte)42)]
    [InlineData("0101", 1, typeof(bool), true)]
    public void Union_IntBool_Reads(string hex, int expectedIndex, Type expectedType, object expectedResult)
    {
        var stream = FromHex(hex);

        var (result, index) = stream.ReadUnion<byte, bool>();

        Assert.Equal(expectedIndex, index);
        Assert.IsType(expectedType, result);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void Union_ThrowsWhenIndexIsInvalid()
    {
        var stream = FromHex("032a");

        Assert.Throws<InvalidDataException>(
            () => stream.ReadUnion<byte, bool>());
    }
}