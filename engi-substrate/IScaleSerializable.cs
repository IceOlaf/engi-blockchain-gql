using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public interface IScaleSerializable
{
    void Serialize(ScaleStreamWriter writer, RuntimeMetadata meta);
}

public static class ScaleSerializableExtensions
{
    public static byte[] Serialize(this IScaleSerializable serializable, RuntimeMetadata meta)
    {
        using var writer = new ScaleStreamWriter();

        serializable.Serialize(writer, meta);

        return writer.GetBytes();
    }
}