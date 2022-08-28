﻿using Engi.Substrate.Metadata.V14;

namespace Engi.Substrate.Server.Indexing;

public class ExpandedBlock
{
    public string Id { get; init; } = null!;

    public ulong Number { get; set; }

    public DateTime? IndexedOn { get; set; }

    public string? Hash { get; set; }

    public string ParentHash { get; set; } = null!;

    public Extrinsic[] Extrinsics { get; set; } = null!;

    public DateTime DateTime { get; set; }

    private ExpandedBlock() { }

    public ExpandedBlock(ulong number)
    {
        Id = KeyFrom(number);
        Number = number;
    }

    public ExpandedBlock(Header header)
        : this(header.Number)
    {
        Hash = header.Hash.Value;
    }

    public void Fill(
        Block block,
        EventRecord[] events,
        RuntimeMetadata meta)
    {
        if (block.Header.Number != Number)
        {
            throw new ArgumentException("Block number doesn't match.", nameof(block));
        }

        Hash = block.Header.Hash.Value;
        ParentHash = block.Header.ParentHash;
        Extrinsics = block.Extrinsics
            .Select(extrinsic => Extrinsic.Parse(extrinsic, meta))
            .ToArray();

        for (var index = 0; index < Extrinsics.Length; index++)
        {
            var extrinsic = Extrinsics[index];

            extrinsic.Events = events
                .Where(x => x.Phase.Data == index)
                .ToArray();
        }

        DateTime = CalculateDateTime(Extrinsics);

        IndexedOn = DateTime.UtcNow;
    }

    public static string KeyFrom(ulong number)
    {
        return $"Blocks/{number:D20}";
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

    public static class MetadataKeys
    {
        public const string SentryId = "SentryId";
    }
}