namespace GroupLab.Core.Gltd.Binary;

/// <summary>
/// Two-of-four erasure shares over GF(256) with polynomial 0x11D, as docs/SPEC-ERRATA.md Q12 defines them:
/// D0, D1, D0 xor D1 and D0 xor 2*D1. Any two distinct shares reconstruct the body.
/// </summary>
internal static class ErasureCoder
{
    public const int ShareCount = 4;
    public const int SharesNeeded = 2;

    private static readonly (byte A, byte B)[] Coefficients = [(1, 0), (0, 1), (1, 1), (1, 2)];
    private static readonly (byte[] Exp, byte[] Log) Tables = BuildTables();

    public static byte[][] Split(ReadOnlySpan<byte> body)
    {
        int half = (body.Length + 1) / 2;
        var d0 = body[..half];
        var d1 = new byte[half];
        body[half..].CopyTo(d1);

        var shares = new byte[ShareCount][];
        for (int s = 0; s < ShareCount; s++)
        {
            var (a, b) = Coefficients[s];
            shares[s] = new byte[half];
            for (int i = 0; i < half; i++)
            {
                shares[s][i] = (byte)(Mul(a, d0[i]) ^ Mul(b, d1[i]));
            }
        }

        return shares;
    }

    /// <summary>Rebuilds D0 followed by D1 from shares <paramref name="i"/> and <paramref name="j"/>.</summary>
    public static byte[] Reconstruct(int i, ReadOnlySpan<byte> shareI, int j, ReadOnlySpan<byte> shareJ)
    {
        if (i == j || shareI.Length != shareJ.Length)
        {
            throw new ArgumentException("Reconstruction needs two distinct shares of equal length.");
        }

        var (a1, b1) = Coefficients[i];
        var (a2, b2) = Coefficients[j];
        byte det = (byte)(Mul(a1, b2) ^ Mul(a2, b1));
        int half = shareI.Length;
        var body = new byte[half * 2];
        for (int k = 0; k < half; k++)
        {
            body[k] = Div((byte)(Mul(shareI[k], b2) ^ Mul(shareJ[k], b1)), det);
            body[half + k] = Div((byte)(Mul(a1, shareJ[k]) ^ Mul(a2, shareI[k])), det);
        }

        return body;
    }

    private static byte Mul(byte a, byte b) =>
        a == 0 || b == 0 ? (byte)0 : Tables.Exp[Tables.Log[a] + Tables.Log[b]];

    private static byte Div(byte a, byte b) =>
        a == 0 ? (byte)0 : Tables.Exp[Tables.Log[a] + 255 - Tables.Log[b]];

    private static (byte[] Exp, byte[] Log) BuildTables()
    {
        var exp = new byte[510];
        var log = new byte[256];
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            exp[i] = (byte)x;
            log[x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0)
            {
                x ^= 0x11D;
            }
        }

        for (int i = 255; i < exp.Length; i++)
        {
            exp[i] = exp[i - 255];
        }

        return (exp, log);
    }
}
