namespace Engi.Substrate;

public static class Era
{
    public static readonly byte[] Immortal = { 0 };

    public static bool IsImmortal(byte[] era) => era.Length == 1 && era[0] == 0;

    private static (ulong period, ulong phase) DecodeEra(ulong current, int period)
    {
        var calPeriod = (ulong)Math.Pow(2, Math.Ceiling(Math.Log2(period)));
        calPeriod = Math.Min(Math.Max(calPeriod, 4), 1 << 16);
        var phase = current % calPeriod;
        var quantizeFactor = Math.Max(calPeriod >> 12, 1);
        var quantizedPhase = phase / quantizeFactor * quantizeFactor;
        return (calPeriod, quantizedPhase);
    }

    private static int GetTrailingZeros(ulong period)
    {
        string binary = Convert.ToString((long)period, 2);

        int index = 0;

        while (binary[binary.Length - 1 - index] == '0')
        {
            index++;
        }

        return index;
    }

    private static byte[] CreateMortalEra(ulong period, ulong phase)
    {
        var quantizeFactor = Math.Max(period >> 12, 1);
        int trailingZeros = GetTrailingZeros(period);
        var encoded = Math.Min(15, (ulong)Math.Max(1, trailingZeros - 1)) + ((phase / quantizeFactor) << 4);
        byte first = (byte)(encoded >> 8);
        byte second = (byte)(encoded & 0xff);
        return new[] { second, first };
    }

    public static byte[] CreateMortal(Header header, int period, int offset = -1)
    {
        var (calPeriod, phase) = DecodeEra((ulong)((int)header.Number + offset), period);

        return CreateMortalEra(calPeriod, phase);
    }
}