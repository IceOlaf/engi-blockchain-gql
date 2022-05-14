using System.Runtime.InteropServices;

namespace Engi.Substrate.Keys;

internal static class SR25519
{
    [DllImport("engi_crypto",
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.Cdecl,
        ExactSpelling = true,
        EntryPoint = "sr25519_keypair_from_seed",
        SetLastError = false)]
    public static extern void KeypairFromSeed(
        byte[] seed,
        [Out] byte[] keypair);

    [DllImport("engi_crypto",
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.Cdecl,
        ExactSpelling = true,
        EntryPoint = "sr25519_sign",
        SetLastError = false)]
    public static extern void Sign(
        byte[] publicKey,
        byte[] secretKey,
        byte[] message,
        uint sz,
        [Out] byte[] signature);

    [DllImport("engi_crypto",
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.Cdecl,
        ExactSpelling = true,
        EntryPoint = "sr25519_verify",
        SetLastError = false)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool Verify(
        byte[] signature,
        byte[] message,
        uint sz,
        byte[] publicKey);
}