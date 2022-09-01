namespace Engi.Substrate;

public static class StorageKeys
{
    public static readonly byte[] Account = Hashing.Twox128("Account");
    public static readonly byte[] Events = Hashing.Twox128("Events");
    public static readonly byte[] Jobs = Hashing.Twox128("Jobs");
    public static readonly byte[] System = Hashing.Twox128("System");

    public static string Blake2Concat(byte[] root, byte[] sub, ulong id)
    {
        byte[] idBytes = BitConverter.GetBytes(id);

        return Hex.ConcatGetOXString(
            root, sub, Hashing.Blake2Concat(idBytes));
    }

    public static string ForJobId(ulong jobId)
    {
        return Blake2Concat(Jobs, Jobs, jobId);
    }
}