using System.Globalization;
using System.Numerics;
using Xunit;

namespace Engi.Substrate;

public class ScaleStreamWriterTests
{
    [Theory]
    [InlineData(00, "00")]
    [InlineData(01, "04")]
    [InlineData(42, "a8")]
    [InlineData(63, "fc")]
    public void CompactInteger_8bit(byte value, string expected)
    {
        AssertCompact(value, expected);
    }

    [Theory]
    [InlineData(64, "0101")]
    [InlineData(69, "1501")]
    [InlineData(16383, "fdff")]
    public void CompactInteger_16bit(ushort value, string expected)
    {
        AssertCompact(value, expected);
    }

    [Theory]
    [InlineData(16384, "02000100")]
    [InlineData(0x01_ff_ff, "feff0700")]
    [InlineData(0x3f_ff_ff_ff, "feffffff")]
    public void CompactInteger_32bit(uint value, string expected)
    {
        AssertCompact(value, expected);
    }
    
    [Theory]
    [InlineData("40_00_00_00", "0300000040")]
    [InlineData("40_50_60_70", "0370605040")]
    [InlineData("ff_00_00_00", "03000000ff")]
    [InlineData("ff_ff_00_00", "030000ffff")]
    [InlineData("ff_ff_ff_ff", "03ffffffff")]
    [InlineData("ff_ff_ff_ff_00", "0700ffffffff")]
    [InlineData("ff_ff_ff_ff_ff", "07ffffffffff")]
    public void CompactInteger_BigInterger(string value, string expected)
    {
        string valueWithoutUnderscores = value.Replace("_", string.Empty);
        var bigInteger = BigInteger.Parse("0" // in C#, BigInteger parsing must start from a 0 to be positive
            + valueWithoutUnderscores, NumberStyles.HexNumber);

        AssertCompact(bigInteger, expected);
    }

    private void AssertCompact(BigInteger value, string expected)
    {
        using var writer = new ScaleStreamWriter();

        writer.WriteCompact(value);

        byte[] bytes = writer.GetBytes();

        string hex = Hex.GetString(bytes);

        Assert.Equal(expected, hex);
    }
}