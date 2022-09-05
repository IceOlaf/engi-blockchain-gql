using Newtonsoft.Json;

namespace Engi.Substrate.Indexing;

public class AddressJsonConverter : JsonConverter<Address>
{
    public override void WriteJson(JsonWriter writer, Address? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.Id);
    }

    public override Address? ReadJson(JsonReader reader, Type objectType, Address? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        string? value = (string?)reader.Value;

        if (value == null)
        {
            return null;
        }

        return Address.Parse(value);
    }
}