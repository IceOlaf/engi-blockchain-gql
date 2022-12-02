using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class ExtrinsicEra : IScaleSerializable, IScaleCalculateLength
{
    public bool IsMortal { get; init; }

    public int? Period { get; init; }

    public int? Phase { get; init; }

    private ExtrinsicEra() { }

    private ExtrinsicEra(int period, int phase)
    {
        IsMortal = true;
        Period = period;
        Phase = phase;
    }

    public static readonly ExtrinsicEra Immortal = new() { IsMortal = false };

    public static ExtrinsicEra CreateMortal(ulong current, int period)
    {
        var calPeriod = (ulong)Math.Pow(2, Math.Ceiling(Math.Log2(period)));

        calPeriod = Math.Min(Math.Max(calPeriod, 4), 1 << 16);

        var phase = current % calPeriod;
        var quantizeFactor = Math.Max(calPeriod >> 12, 1);
        var quantizedPhase = phase / quantizeFactor * quantizeFactor;

        return new((int)calPeriod, (int)quantizedPhase);
    }

    public static ExtrinsicEra CreateMortal(Header header, int period, int offset = -1)
    {
        return CreateMortal((ulong)((int)header.Number + offset), period);
    }

    public static ExtrinsicEra Parse(ScaleStreamReader reader)
    {
        byte first = (byte)reader.ReadByte();

        if (first == 0)
        {
            return Immortal;
        }

        byte second = (byte)reader.ReadByte();

        int encoded = first + (second << 8);
        int period = 2 << encoded % (1 << 4);
        int quantizeFactor = Math.Max(period >> 12, 1);
        int phase = (encoded >> 4) * quantizeFactor;

        return new ExtrinsicEra(period, phase);
    }

    public void Serialize(ScaleStreamWriter writer, RuntimeMetadata _)
    {
        if (IsMortal == false)
        {
            writer.Write(SerializedImmortalEra);
            return;
        }

        var quantizeFactor = Math.Max(Period!.Value >> 12, 1);
        int trailingZeros = GetTrailingZeros(Period.Value);
        var encoded = Math.Min(15, Math.Max(1, trailingZeros - 1)) + ((Phase!.Value / quantizeFactor) << 4);
        
        byte first = (byte)(encoded >> 8);
        byte second = (byte)(encoded & 0xff);
        
        writer.Write(second);
        writer.Write(first);
    }

    public int CalculateLength()
    {
        return IsMortal ? 2 : 1;
    }

    private static int GetTrailingZeros(int period)
    {
        string binary = Convert.ToString(period, 2);

        int index = 0;

        while (binary[binary.Length - 1 - index] == '0')
        {
            index++;
        }

        return index;
    }

    private static readonly byte[] SerializedImmortalEra = { 0 };
}