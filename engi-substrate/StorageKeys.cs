namespace Engi.Substrate;

public static class StorageKeys
{
    private static byte[] Blake2_128Concat(ulong x)
    {
        return Hashing.Blake2Concat(BitConverter.GetBytes(x));
    }

    private static byte[] Blake2_128Concat(string s)
    {
        using var writer = new ScaleStreamWriter();

        writer.Write(s);

        return Hashing.Blake2Concat(writer.GetBytes());
    }

    public static string Blake2Concat(byte[] root, byte[] sub, ulong id)
    {
        return Hex.ConcatGetOXString(
            root, sub, Blake2_128Concat(id));
    }

    public static class System
    {
        private static readonly byte[] Prefix = Hashing.Twox128("System");
        private static readonly string AccountPrefix = Hex.ConcatGetOXString(Prefix, Hashing.Twox128("Account"));

        public static readonly Func<Address, string> Account = 
            address => AccountPrefix + Hex.GetString(Hashing.Blake2Concat(address.Raw));
        
        public static readonly string Events = Hex.ConcatGetOXString(Prefix, Hashing.Twox128("Events"));
    }

    public static class Jobs
    {
        private static readonly byte[] Prefix = Hashing.Twox128("Jobs");
        private static readonly byte[] Solutions = Hashing.Twox128("Solutions");

        public static string ForJobId(ulong jobId)
        {
            return Blake2Concat(Prefix, Prefix, jobId);
        }

        public static string ForTestSolution(ulong jobId, string testId)
        {
            return Hex.ConcatGetOXString(
                Prefix, 
                Solutions, 
                Blake2_128Concat(jobId),
                Blake2_128Concat(testId));
        }
    }
}