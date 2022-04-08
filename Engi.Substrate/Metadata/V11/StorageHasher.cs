namespace Engi.Substrate.Metadata.V11;

public enum StorageHasher
{
    Blake2_128 = 0,
    Blake2_256 = 1,
    Blake2_128Concat = 2,
    Twox128 = 3,
    Twox256 = 4,
    Twox64Concat = 5,
    Identity = 6,
}