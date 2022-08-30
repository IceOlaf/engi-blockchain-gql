namespace Engi.Substrate;

public static class ChainKeys
{
    public const string AuthorSubmitExtrinsic = "author_submitExtrinsic";

    public const string ChainFinalizedHead = "chain_finalizedHead";
    public const string ChainGetBlock = "chain_getBlock";
    public const string ChainGetBlockHash = "chain_getBlockHash";
    public const string ChainGetFinalizedHead = "chain_getFinalizedHead";
    public const string ChainGetHeader = "chain_getHeader";
    public const string ChainNewHead = "chain_newHead";
    public const string ChainSubscribeNewHead = "chain_subscribeNewHead";
    public const string ChainSubscribeFinalizedHeads = "chain_subscribeFinalizedHeads";

    public const string SystemChain = "system_chain";
    public const string SystemName = "system_name";
    public const string SystemVersion = "system_version";
    public const string SystemHealth = "system_health";

    public const string StateGetMetadata = "state_getMetadata";
    public const string StateGetStorage = "state_getStorage";
    public const string StateSubscribeRuntimeVersion = "state_subscribeRuntimeVersion";
    public const string StateRuntimeVersion = "state_runtimeVersion";

    public static class Pallets
    {
        public static class Jobs
        {
            public const string Name = "Jobs";

            public const string JobIdGeneratedEvent = "JobIdGenerated";
            public const string JobAttemptedEvent = "JobAttempted";
        }
    }
}