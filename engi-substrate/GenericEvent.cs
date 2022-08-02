using System.Collections;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class GenericEvent
{
    public string Section { get; set; } = null!;

    public string Method { get; set; } = null!;

    public byte[] Index { get; set; } = null!;

    public object Data { get; set; } = null!;

    public string[] DataKeys
    {
        get
        {
            var dataType = Data.GetType();

            if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keysProperty = dataType.GetProperty("Keys");

                var keys = keysProperty!.GetValue(Data)!;

                switch (keys)
                {
                    case ICollection<string> stringKeys:
                        return stringKeys.ToArray();
                    case ICollection<int> integerKeys:
                        return integerKeys.Select(i => i.ToString()).ToArray();
                    default: 
                        throw new NotImplementedException();
                }
            }

            return Array.Empty<string>();
        }
    }

    public static GenericEvent Parse(ScaleStreamReader reader, RuntimeMetadata meta)
    {
        var index = reader.ReadFixedSizeByteArray(2);

        var (module, eventVariant) = meta.FindEvent(index);

        return new()
        {
            Section = module.Name,
            Method = eventVariant.Name,
            Index = index,
            Data = reader.Deserialize(eventVariant.Fields, meta)
        };
    }
}