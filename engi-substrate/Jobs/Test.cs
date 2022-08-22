namespace Engi.Substrate.Jobs;

public class Test : IScaleSerializable
{
    public string Id { get; init; } = null!;

    public TestResult Result { get; init; }

    public string ResultMessage { get; init; } = string.Empty;

    public TestResult Required { get; init; }

    public string RequiredMessage { get; init; } = string.Empty;

    public void Serialize(ScaleStreamWriter writer)
    {
        writer.Write(Id);
        writer.Write(Result);
        writer.Write(ResultMessage);
        writer.Write(Required);
        writer.Write(RequiredMessage);
    }
}