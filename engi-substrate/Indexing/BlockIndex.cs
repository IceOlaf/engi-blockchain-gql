using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Indexing;

public class BlockIndex : AbstractIndexCreationTask<ExpandedBlock>
{
    public class Result
    {
        public ulong Number { get; set; }

        public string?Hash { get; set; }

        public DateTime? IndexedOn { get; set; }

        public string? SentryId { get; set; }
    }

    public BlockIndex()
    {

        Map = blocks => from block in blocks
            select new Result
            {
                Number = block.Number,
                Hash = block.Hash,
                IndexedOn = block.IndexedOn,
                SentryId = block.SentryId
            };

        Priority = IndexPriority.Low;
    }
}
