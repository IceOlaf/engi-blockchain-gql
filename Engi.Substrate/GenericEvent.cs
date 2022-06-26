using System.Dynamic;
using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class GenericEvent
{
    public string Section { get; set; } = null!;

    public string Method { get; set; } = null!;

    public byte[] Index { get; set; } = null!;

    public ExpandoObject Data { get; set; } = null!;

    public static GenericEvent Parse(ScaleStreamReader reader, RuntimeMetadata meta)
    {
        var index = reader.ReadFixedSizeByteArray(2);

        var (module, eventVariant) = meta.FindEvent(index);

        return new()
        {
            Section = module.Name,
            Method = eventVariant.Name,
            Index = index,
            Data = reader.DeserializeDynamicType(eventVariant, meta)
        };
    }
}