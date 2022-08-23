namespace Engi.Substrate.Jobs;

public class Repository
{
    public string Url { get; set; } = null!;

    public string Branch { get; set; } = null!;

    public string Commit { get; set; } = null!;

    public static Repository Parse(ScaleStreamReader reader)
    {
        return new()
        {
            Url = reader.ReadString()!,
            Branch = reader.ReadString()!,
            Commit = reader.ReadString()!
        };
    }
}