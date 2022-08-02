using Newtonsoft.Json;

namespace Engi.Substrate.Server.Indexing;

public class InlineByteArrayJsonConvert : JsonConverter<byte[]>
{
    public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (value.Length > 10)
        {
            string s = Convert.ToBase64String(value);
            
            writer.WriteValue(s);
            
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteValue(item);
        }
        writer.WriteEndArray();
    }

    public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.String)
        {
            return Convert.FromBase64String((string)reader.Value!);
        }

        // array

        using var ms = new MemoryStream();

        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            ms.WriteByte(Convert.ToByte(reader.Value!));
        }

        return ms.ToArray();
    }
}