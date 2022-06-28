using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engi.Substrate;

public class ScaleCompactIntegerJsonConverter : JsonConverter<object>
{
    private static readonly Type[] SupportedTypes =
    {
        typeof(byte),
        typeof(ushort),
        typeof(uint),
        typeof(ulong)
    };

    public override bool CanConvert(Type t) => SupportedTypes.Contains(t);

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string hex = reader.GetString()!;

        using var stream = new ScaleStreamReader(hex);

        return stream.ReadCompactInteger();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) => throw new NotImplementedException();
}