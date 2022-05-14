using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engi.Substrate;

public class ScaleJsonConverter : JsonConverter<object>
{
    private static readonly Type[] SupportedTypes =
    {
        typeof(bool?),
        typeof(BigInteger)
    };

    private static bool IsSupported(Type t)
    {
        return t.IsPrimitive
               || t.IsEnum
               || SupportedTypes.Contains(t);
    }

    public override bool CanConvert(Type t)
    {
        return IsSupported(t)
               || t.IsGenericType
               && t.GetGenericTypeDefinition() == typeof(List<>)
               && IsSupported(t.GetGenericArguments().First());
    }

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string hex = reader.GetString()!;

        using var stream = new ScaleStreamReader(hex);

        return stream.Read(typeToConvert);
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) => throw new NotImplementedException();
}