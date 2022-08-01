using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate;

public class EventRecord
{
    public Phase Phase { get; set; } = null!;

    public GenericEvent Event { get; set; } = null!;

    public string[] Topics { get; set; } = null!;

    public static EventRecord Parse(ScaleStreamReader reader, RuntimeMetadata meta)
    {
        return new()
        {
            Phase = new()
            {
                Value = reader.ReadEnum<PhaseType>(),
                Data = reader.ReadUInt32()
            },
            Event = GenericEvent.Parse(reader, meta),
            Topics = reader.ReadList(s => s.ReadString())!
        };
    }

    public static EventRecord Parse(string s, RuntimeMetadata meta)
    {
        var reader = new ScaleStreamReader(s);

        return Parse(reader, meta);
    }
}