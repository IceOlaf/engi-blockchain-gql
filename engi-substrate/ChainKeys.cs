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
    public const string StateQueryStorageAt = "state_queryStorageAt";
    public const string StateSubscribeRuntimeVersion = "state_subscribeRuntimeVersion";
    public const string StateRuntimeVersion = "state_runtimeVersion";

    public static class Balances
    {
        public const string Name = "Balances";

        public static class Calls
        {
            public const string Transfer = "transfer";
        }
    }

    public static class Jobs
    {
        public const string Name = "Jobs";

        public static class Calls
        {
            public const string CreateJob = "create_job";
            public const string AttemptJob = "attempt_job";
            public const string SolveJob = "solve_job";
        }

        public static class Events
        {
            public const string JobIdGenerated = "JobIdGenerated";
            public const string JobAttempted = "JobAttempted";
        }
    }

    public static class Sudo
    {
        public const string Name = "Sudo";

        public static class Calls
        {
            public const string Call = "sudo";
        }
    }
}