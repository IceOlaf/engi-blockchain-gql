namespace Engi.Substrate;

public class DataEnum<TEnum, TValue> where TEnum : Enum
{
    public TEnum Value { get; set; } = default!;

    public TValue Data { get; set; } = default!;
}