namespace Engi.Substrate.Jobs;

public class Solution
{
    public ulong SolutionId { get; set; }

    public ulong JobId { get; set; }

    public string Author { get; set; } = null!;

    public string PatchUrl { get; set; } = null!;

    public Attempt Attempt { get; set; } = null!;

    public static Solution Parse(ScaleStreamReader reader)
    {
        return new()
        {
            SolutionId = reader.ReadUInt64(),
            JobId = reader.ReadUInt64(),
            Author = reader.ReadAddressAsId(),
            PatchUrl = reader.ReadString()!,
            Attempt = Attempt.Parse(reader)
        };
    }
}