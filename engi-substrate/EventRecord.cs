using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class EventRecord
{
    public Phase Phase { get; set; } = null!;

    public GenericEvent Event { get; set; } = null!;

    public string[] Topics { get; set; } = null!;

    public static EventRecord Parse(ScaleStreamReader reader, RuntimeMetadata meta)
    {
        var phase = new Phase
        {
            Value = reader.ReadEnum<PhaseType>()
        };

        phase.Data = phase.Value == PhaseType.ApplyExtrinsic ? reader.ReadUInt32() : null;

        return new EventRecord
        {
            Phase = phase,
            Event = GenericEvent.Parse(reader, meta),
            Topics = reader.ReadList(s => s.ReadString())!
        };
    }

    public static EventRecord Parse(string s, RuntimeMetadata meta)
    {
        var reader = new ScaleStreamReader(s);

        return Parse(reader, meta);
    }

    public override string? ToString() => Event.ToString();
}
