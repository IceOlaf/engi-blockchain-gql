using Newtonsoft.Json;

namespace Engi.Substrate.Indexing;

public class UInt64JsonConverter : JsonConverter<ulong>
{
    public override void WriteJson(JsonWriter writer, ulong value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString(StorageFormats.UInt64));
    }

    public override ulong ReadJson(JsonReader reader, Type objectType, ulong existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.Value is string s)
        {
            return ulong.Parse(s);
        }

        return Convert.ToUInt64(reader.Value!);
    }
}