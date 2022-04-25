using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engi.Substrate;

public class HexUInt64JsonConverter : JsonConverter<object>
{
    public override bool CanConvert(Type t) => t == typeof(ulong);

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string hex = reader.GetString()!;

        return ulong.Parse(hex.Substring(2), NumberStyles.HexNumber);
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) => throw new NotImplementedException();
}