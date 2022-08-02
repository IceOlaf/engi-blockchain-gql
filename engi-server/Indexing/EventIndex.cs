using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Server.Indexing;

public class EventIndex : AbstractIndexCreationTask
{
    public class Result
    {
        public ulong Number { get; set; }

        public string Hash { get; set; } = null!;

        public DateTime DateTime { get; set; }

        public string ExtrinsicPallet { get; set; } = null!;

        public string ExtrinsicCall { get; set; } = null!;

        public string Executor { get; set; } = null!;

        public bool IsSuccessful { get; set; }

        public string EventSection { get; set; } = null!;

        public string EventMethod { get; set; } = null!;
    }

    public override IndexDefinition CreateIndexDefinition()
    {
        return new()
        {
            Maps = new()
            {
                @"
                    from block in docs.ExpandedBlocks
                    from extrinsic in block.Extrinsics
                    from @event in extrinsic.Events
                    select new
                    {
                        block.Number,
                        block.Hash,
                        block.DateTime,
                        ExtrinsicPallet = extrinsic.PalletName,
                        ExtrinsicCall = extrinsic.CallName,
                        Executor = extrinsic.Signature.Address.Value,
                        IsSuccessful = extrinsic.Events.Any(e => e.Event.Section == ""System"" && e.Event.Method == ""ExtrinsicSuccess""),
                        EventSection = @event.Event.Section,
                        EventMethod = @event.Event.Method,
                        _ = @event.Event.DataKeys.Select(key => this.CreateField(""EventData_"" + key, @event.Event.Data[key], false, false))
                    }
                "
            },
            Fields = new()
            {
                ["__all_fields"] = new IndexFieldOptions
                {
                    Storage = FieldStorage.Yes,
                    Indexing = FieldIndexing.Exact
                }
            }
        };
    }
}