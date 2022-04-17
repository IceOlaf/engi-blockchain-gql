namespace Engi.Substrate;

public static class Base58Encoding
{
    private static string Base58characters = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public static byte[] Decode(string source)
    {
        int i = 0;

        // skip leading zeros

        while (i < source.Length)
        {
            if (source[i] == 0 || !char.IsWhiteSpace(source[i]))
            {
                break;
            }
            i++;
        }

        // count zeros as '1'
        int zeros = 0;

        while (source[i] == '1')
        {
            zeros++;
            i++;
        }

        // map bytes

        byte[] b256 = new byte[(source.Length - i) * 733 / 1000 + 1];

        while (i < source.Length && !char.IsWhiteSpace(source[i]))
        {
            int ch = Base58characters.IndexOf(source[i]);

            if (ch == -1)
            {
                throw new InvalidDataException();
            }

            int carry = Base58characters.IndexOf(source[i]);

            for (int k = b256.Length - 1; k >= 0; k--)
            {
                carry += 58 * b256[k];
                b256[k] = (byte)(carry % 256);
                carry /= 256;
            }

            i++;
        }

        while (i < source.Length && char.IsWhiteSpace(source[i]))
        {
            i++;
        }

        if (i != source.Length)
        {
            throw new InvalidDataException();
        }

        int j = 0;

        while (j < b256.Length && b256[j] == 0)
        {
            j++;
        }

        byte[] destination = new byte[zeros + (b256.Length - j)];

        for (int kk = 0; kk < destination.Length; kk++)
        {
            destination[kk] = kk < zeros ? (byte)0x00 : b256[j++];
        }

        return destination;
    }
}