namespace Engi.Substrate;

public static class StorageKeys
{
    public static readonly byte[] Account = Hashing.Twox128("Account");
    public static readonly byte[] Events = Hashing.Twox128("Events");
    public static readonly byte[] Jobs = Hashing.Twox128("Jobs");
    public static readonly byte[] System = Hashing.Twox128("System");
}