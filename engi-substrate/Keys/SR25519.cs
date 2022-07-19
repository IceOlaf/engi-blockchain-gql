using System.Runtime.InteropServices;

namespace Engi.Substrate.Keys;

internal static class SR25519
{
    static SR25519()
    {
        NativeLibrary.SetDllImportResolver(typeof(SR25519).Assembly,
            (name, assembly, path) =>
            {
                string basePath = Path.GetDirectoryName(assembly.Location)!;

                IntPtr Load(string filename)
                {
                    return NativeLibrary.Load(
                        Path.Combine(basePath, "lib", filename));
                }

                if (name == "engi_crypto")
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                        {
                            return Load("libengi_crypto_arm64.dylib");
                        }

                        return Load("libengi_crypto.dylib");
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                        {
                            return Load("libengi_crypto_arm64.so");
                        }

                        return Load("libengi_crypto.so");
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        && RuntimeInformation.OSArchitecture == Architecture.X64)
                    {
                        return Load("engi_crypto.dll");
                    }

                    throw new NotSupportedException(
                        $"The combination of OSPlatform={GetOSPlatform()} OSArchitecture={RuntimeInformation.OSArchitecture} and  is not supported.");
                }

                return IntPtr.Zero;
            });
    }

    private static OSPlatform GetOSPlatform()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
        {
            return OSPlatform.Windows;
        }

        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OSPlatform.Linux;
        }

        if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }

        if(RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            return OSPlatform.FreeBSD;
        }

        throw new NotImplementedException($"Platform: {RuntimeInformation.OSDescription}");
    }

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
        EntryPoint = "sr25519_keypair_from_secret",
        SetLastError = false)]
    public static extern void KeypairFromSecretKey(
        byte[] secretKey,
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