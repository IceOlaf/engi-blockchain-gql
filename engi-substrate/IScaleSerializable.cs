namespace Engi.Substrate;

public interface IScaleSerializable
{
    void Serialize(ScaleStreamWriter writer);
}