using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Server.Indexing;

public class ExpandedBlock
{
    public string Id { get; init; } = null!;

    public ulong Number { get; set; }

    public DateTime? IndexedOn { get; set; }

    public string? Hash { get; set; }

    public string ParentHash { get; set; } = null!;

    public Extrinsic[] Extrinsics { get; set; } = null!;

    public EventRecordCollection Events { get; set; } = null!;

    public DateTime DateTime { get; set; }

    public string? PreviousId { get; set; }

    public string? SentryId { get; set; }

    private ExpandedBlock() { }

    public ExpandedBlock(ulong number)
    {
        Id = KeyFrom(number);
        Number = number;

        if (number > 1)
        {
            PreviousId = KeyFrom(number - 1);
        }
    }

    public ExpandedBlock(ulong number, string hash)
        : this(number)
    {
        Hash = hash;
    }

    public ExpandedBlock(Header header)
        : this(header.Number, header.Hash.Value)
    { }

    public void Fill(
        Block block,
        EventRecord[] events,
        RuntimeMetadata meta)
    {
        if (block == null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        if (block.Header.Number != Number)
        {
            throw new ArgumentException("Block number doesn't match.", nameof(block));
        }

        if (block.Extrinsics == null)
        {
            throw new ArgumentNullException(nameof(Block.Extrinsics));
        }

        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        Hash = block.Header.Hash.Value;
        ParentHash = block.Header.ParentHash;
        Extrinsics = block.Extrinsics
            .Select(extrinsic => Extrinsic.Parse(extrinsic, meta))
            .ToArray();
        Events = new EventRecordCollection(events
            .Where(x => x.Phase.Value != PhaseType.ApplyExtrinsic)
            .ToArray());

        for (var index = 0; index < Extrinsics.Length; index++)
        {
            var extrinsic = Extrinsics[index];

            extrinsic.Events = new EventRecordCollection(events
                .Where(x => x.Phase.Value == PhaseType.ApplyExtrinsic && x.Phase.Data == index)
                .ToArray());
        }

        DateTime = CalculateDateTime(Extrinsics);
    }

    public static string KeyFrom(ulong number)
    {
        return $"Blocks/{number.ToString(StorageFormats.UInt64)}";
    }

    public static implicit operator BlockReference(ExpandedBlock block)
    {
        return new()
        {
            Number = block.Number,
            DateTime = block.DateTime
        };
    }

    // helpers

    private static DateTime CalculateDateTime(Extrinsic[] extrinsics)
    {
        var setTimeExtrinsic = extrinsics
            .SingleOrDefault(x => x.PalletName == "Timestamp" && x.CallName == "set");

        if (setTimeExtrinsic == null)
        {
            throw new InvalidOperationException("Block does not contain Timestamp.set() extrinsic");
        }

        return (DateTime)setTimeExtrinsic.Arguments["now"];
    }
}
