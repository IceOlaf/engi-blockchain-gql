using Raven.Client.Documents.Indexes;

namespace Engi.Substrate.Indexing;

public class TransactionIndex : AbstractIndexCreationTask
{
    public class Result
    {
        public ulong Number { get; set; }

        public string Hash { get; set; } = null!;

        public DateTime DateTime { get; set; }

        public TransactionType Type { get; set; }

        public string Executor { get; set; } = null!;

        public bool IsSuccessful { get; set; }

        public string[]? OtherParticipants { get; set; }

        public decimal Amount { get; set; }

        public string? JobId { get; set; }
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
                    let type = IndexUtils.GetTransactionType(extrinsic.PalletName, extrinsic.CallName, extrinsic.Arguments)
                    where type != null
                    select new
                    {
                        block.Number,
                        block.Hash,
                        block.DateTime,
                        Type = type,
                        Executor = extrinsic.Signature.Address.Value,
                        IsSuccessful = IndexUtils.GetIsSuccessful(extrinsic.PalletName, extrinsic.Events),
                        OtherParticipants = IndexUtils.GetOtherParticipants(type, extrinsic.Arguments),
                        Amount = IndexUtils.GetAmount(type, extrinsic.Arguments, extrinsic.CallName),
                        JobId = IndexUtils.GetJobId(type, extrinsic.Arguments, extrinsic.Events)
                    }
                "
            },

            AdditionalSources = new Dictionary<string, string>
            {
                ["IndexUtils"] = @"
using System;
using System.Collections.Generic;
using static Engi.Substrate.Server.Indexing.IndexUtils;

namespace Engi.Substrate.Server.Indexing
{
    public enum TransactionType
    {
        Exchange,
        Transfer,
        Spend,
        Income
    }

    public static class IndexUtils
    {
        public static bool GetIsSuccessful(string pallet, dynamic events)
        {
            if(pallet == ""Sudo"")
            {
                return events.Any((Func<dynamic, bool>)(e => e.Event.Section == ""Sudo"" && e.Event.Method == ""Sudid"" && e.Event.DataKeys.Contains(""Ok"")));
            }
            
            return events.Any((Func<dynamic, bool>)(e => e.Event.Section == ""System"" && e.Event.Method == ""ExtrinsicSuccess""));
        }

        public static TransactionType? GetTransactionType(string pallet, string call, dynamic args)
        {
            if(pallet == ""Jobs"")
            {
                if(call == ""create_job"")
                {
                    return TransactionType.Spend;
                }

                return null;
            }

            if(pallet == ""Sudo"" && call == ""sudo"")
            {   
                var sudoCall = args.call?.Jobs?.solve_job;

                if(sudoCall != null)
                {
                    return TransactionType.Income;
                }

                return null;
            }

            if(pallet == ""Exchange"" && call == ""sell"")
            {
                return TransactionType.Exchange;
            }

            if(pallet == ""ChainBridge"" && call == ""acknowledge_proposal"")
            {
                var innerCall = args.call?.Exchange?.transfer;

                if(innerCall != null)
                {
                    return TransactionType.Exchange;
                }
            }

            if(pallet == ""Balances"")
            {
                if(call == ""transfer_keep_alive"")
                {
                    return TransactionType.Transfer;
                }
            }

            return null;
        }

        public static string[] GetOtherParticipants(TransactionType type, dynamic args)
        {
            if(type == TransactionType.Transfer)
            {
                dynamic dest = args[""dest""];
                
                return new string[] { (string) dest.Value }; 
            }
                
            return new string[0];
        }

        public static decimal GetAmount(TransactionType type, dynamic args, string callName)
        {
            if(type == TransactionType.Exchange)
            {
                if(callName == ""sell"")
                {
                    return -decimal.Parse((string) args.amount);
                }

                return decimal.Parse((string) args.call.Exchange.transfer[1]);
            }

            if(type == TransactionType.Transfer)
            {
                return decimal.Parse((string) args.value);
            }

            if(type == TransactionType.Spend)
            {
                return decimal.Parse((string) args.funding);
            }

            return 0;
        }

        public static ulong GetJobId(TransactionType type, dynamic args, dynamic events)
        {
            if(type == TransactionType.Spend)
            {
                var jobIdGeneratedEvent = events
                    .FirstOrDefault((Func<dynamic, bool>)(x => x.Event.Section == ""Jobs"" && x.Event.Method == ""JobIdGenerated""));

                if(jobIdGeneratedEvent == null)
                {
                    return null;
                }

                return jobIdGeneratedEvent.Event.Data;
            }
            
            if(type == TransactionType.Income)
            {
                return args.call.Jobs.solve_job.job;
            }

            return null;
        }
    }
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
