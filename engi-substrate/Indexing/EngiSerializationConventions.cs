using Newtonsoft.Json;
using Raven.Client.Json.Serialization.NewtonsoftJson;

namespace Engi.Substrate.Indexing;

public class EngiSerializationConventions : NewtonsoftJsonSerializationConventions
{
    public EngiSerializationConventions()
    {
        CustomizeJsonSerializer = CustomizeSerializer;
        CustomizeJsonDeserializer = CustomizeSerializer;
    }

    private static void CustomizeSerializer(JsonSerializer serializer)
    {
        serializer.Converters.Add(new AddressJsonConverter());
        serializer.Converters.Add(new BigIntegerJsonConverter());
        serializer.Converters.Add(new InlineByteArrayJsonConvert());
        serializer.Converters.Add(new UInt64JsonConverter());
    }
}