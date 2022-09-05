namespace Engi.Substrate;

public interface IScaleSerializable
{
    void Serialize(ScaleStreamWriter writer);
}

public static class ScaleSerializableExtensions
{
    public static byte[] Serialize(this IScaleSerializable serializable)
    {
        using var writer = new ScaleStreamWriter();

        serializable.Serialize(writer);

        return writer.GetBytes();
    }
}