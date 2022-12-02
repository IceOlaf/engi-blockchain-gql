using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Engi.Substrate.Keys;

public class KeypairFactory
{
    private static string[] GetMnemonicWords(string mnemonic) => mnemonic
        .Normalize(NormalizationForm.FormKD)
        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    public static void ValidateMnemonic(string mnemonic, string[] wordlist, out int[]? validatedMnemonicIndices)
    {
        validatedMnemonicIndices = null;

        bool isRaw = mnemonic.StartsWith("0x");

        if (isRaw)
        {
            string raw = mnemonic.Substring(2);

            if (raw.Contains(' '))
            {
                throw new ArgumentException(
                    "The mnemonic was treated as raw seed but contains in invalid hex value.");
            }

            if (raw.Length > 64)
            {
                throw new ArgumentException(
                    "The mnemonic was treated as raw seed but exceeds 32 bytes in length.",
                    nameof(mnemonic));
            }
        }
        else
        {
            var mnemonicWords = GetMnemonicWords(mnemonic);

            if (mnemonicWords.Length is not 12 or 15 or 18 or 21 or 24)
            {
                throw new ArgumentException(
                    "Invalid mnemonic; must be 12, 15, 18, 21 or 24 words long.",
                    nameof(mnemonic));
            }

            var tempMnemonicIndices = mnemonicWords
                .Select(word => Array.IndexOf(wordlist, word))
                .ToArray();

            if (tempMnemonicIndices.Any(idx => idx == -1))
            {
                throw new ArgumentException(
                    "Invalid mnemonic; some words were not contained in the wordlist.");
            }

            validatedMnemonicIndices = tempMnemonicIndices;
        }
    }

    public static Keypair CreateFromAny(string mnemonic, string? mnemonicSalt = null)
    {
        ValidateMnemonic(mnemonic, Wordlists.English, out int[]? validatedMnemonicIndices);

        if (validatedMnemonicIndices == null)
        {
            // hex strings contain double the length

            string raw = mnemonic.Substring(2); // remove0 x

            byte[] miniSecretOrPrivateKey = Hex.GetBytes(raw.PadLeft(32 * 2));

            return CreateFromSeed(miniSecretOrPrivateKey);
        }

        return CreateFromMnemonic(validatedMnemonicIndices, mnemonicSalt ?? string.Empty);
    }

    private static Keypair CreateFromSeed(byte[] seed)
    {
        ThrowIfEntropyIsInvalid(seed);

        var keypairBytes = new byte[Keypair.KEYPAIR_LENGTH];

        SR25519.KeypairFromSeed(seed, keypairBytes);

        var publicKey = keypairBytes
            .AsSpan(Keypair.SECRET_KEY_LENGTH, Keypair.PUBLIC_KEY_LENGTH).ToArray();

        return new Keypair
        {
            Address = Address.From(publicKey),
            SecretKey = keypairBytes.AsSpan(0, Keypair.SECRET_KEY_LENGTH).ToArray(),
            PublicKey = publicKey
        };
    }

    private static Keypair CreateFromMnemonic(int[] mnemonicIndices, string password)
    {
        var seed = CreateSeedFromWordlistMnemonic(mnemonicIndices, password);

        return CreateFromSeed(seed);
    }

    private static byte[] CreateSeedFromWordlistMnemonic(int[] mnemonicIndices, string password)
    {
        var entropy = MnemonicToEntropy(mnemonicIndices);

        var saltBytes = Encoding.UTF8.GetBytes("mnemonic" + password);

        var seed = PBKDF2Sha512GetBytes(64, entropy, saltBytes, 2048);

        return seed.AsSpan(0, 32).ToArray();
    }

    private static byte[] MnemonicToEntropy(int[] mnemonicIndices)
    {
        var bitBuilder = new StringBuilder();

        foreach (var index in mnemonicIndices)
        {
            bitBuilder.Append(Convert.ToString(index, 2)
                .PadLeft(11, '0'));
        }

        var bits = bitBuilder.ToString();

        // split the binary string into ENT/CS

        var dividerIndex = (int)Math.Floor((double)bitBuilder.Length / 33) * 32;
        var entropyBits = bits.Substring(0, dividerIndex);
        var checksumBits = bits.Substring(dividerIndex);

        // calculate the checksum and compare

        var entropyBytes = Regex.Matches(entropyBits, "(.{1,8})")
            .Select(m => m.Groups[0].Value)
            .Select(bytes => Convert.ToByte(bytes, 2))
            .ToArray();

        ThrowIfEntropyIsInvalid(entropyBytes);

        var newChecksum = DeriveChecksumBits(entropyBytes);

        if (newChecksum != checksumBits)
        {
            throw new DataException("Invalid checksum");
        }

        return entropyBytes;
    }

    private static void ThrowIfEntropyIsInvalid(byte[] entropyBytes)
    {
        if (entropyBytes.Length is < 16 or > 32)
        {
            throw new DataException("Invalid entropy");
        }

        if (entropyBytes.Length % 4 != 0)
        {
            throw new DataException("Invalid entropy");
        }
    }

    private static string DeriveChecksumBits(byte[] checksum)
    {
        int ent = checksum.Length * 8;
        int cs = ent / 32;

        var sha256Provider = SHA256.Create();

        var hash = sha256Provider.ComputeHash(checksum);

        // this was a weird part of the code where it would create the binary representation
        // of the hash but then use only the first 4 binary digits :shrug:
        // I optimized by only converting the first byte but just to be sure added
        // a loop in case in some cases it needs more chars, though unlikely

        string bits = Convert.ToString(hash[0], 2).PadLeft(8, '0');

        int index = 1;

        while (bits.Length < cs)
        {
            bits += Convert.ToString(hash[index++], 2).PadLeft(8, '0');
        }

        return bits.Substring(0, cs);
    }

    private static byte[] PBKDF2Sha512GetBytes(int dklen, byte[] password, byte[] salt, int iterationCount)
    {
        using var hmac = new HMACSHA512(password);

        int hashLength = hmac.HashSize / 8;

        if ((hmac.HashSize & 7) != 0)
        {
            hashLength++;
        }

        int keyLength = dklen / hashLength;

        if (dklen > 0xFFFFFFFFL * hashLength || dklen < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dklen));
        }

        if (dklen % hashLength != 0)
        {
            keyLength++;
        }

        byte[] extendedkey = new byte[salt.Length + 4];

        Buffer.BlockCopy(salt, 0, extendedkey, 0, salt.Length);

        using var ms = new MemoryStream();

        for (int i = 0; i < keyLength; i++)
        {
            extendedkey[salt.Length] = (byte)(((i + 1) >> 24) & 0xFF);
            extendedkey[salt.Length + 1] = (byte)(((i + 1) >> 16) & 0xFF);
            extendedkey[salt.Length + 2] = (byte)(((i + 1) >> 8) & 0xFF);
            extendedkey[salt.Length + 3] = (byte)((i + 1) & 0xFF);

            byte[] u = hmac.ComputeHash(extendedkey);

            Array.Clear(extendedkey, salt.Length, 4);

            byte[] f = u;

            for (int j = 1; j < iterationCount; j++)
            {
                u = hmac.ComputeHash(u);

                for (int k = 0; k < f.Length; k++)
                {
                    f[k] ^= u[k];
                }
            }

            ms.Write(f, 0, f.Length);

            Array.Clear(u, 0, u.Length);
            Array.Clear(f, 0, f.Length);
        }

        Array.Clear(extendedkey, 0, extendedkey.Length);

        return ms.ToArray();
    }
}