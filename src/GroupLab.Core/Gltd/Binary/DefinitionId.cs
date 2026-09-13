using System.Security.Cryptography;

namespace GroupLab.Core.Gltd.Binary;

/// <summary>
/// The definition identifier of TARGET-SCHEMA.md section 6: the first ten bytes of the SHA-256 of the
/// canonical body, as sixteen Crockford base-32 characters grouped in fours.
/// </summary>
public static class DefinitionId
{
    public const int ByteLength = 10;

    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string Compute(ReadOnlySpan<byte> body) => Format(SHA256.HashData(body).AsSpan(0, ByteLength));

    public static string Format(ReadOnlySpan<byte> idBytes)
    {
        if (idBytes.Length != ByteLength)
        {
            throw new ArgumentException($"An identifier is {ByteLength} bytes, got {idBytes.Length}.", nameof(idBytes));
        }

        UInt128 n = 0;
        foreach (byte b in idBytes)
        {
            n = (n << 8) | b;
        }

        Span<char> chars = stackalloc char[16];
        for (int i = 15; i >= 0; i--)
        {
            chars[i] = Alphabet[(int)(n & 31)];
            n >>= 5;
        }

        return $"GL-{chars[..4]}-{chars[4..8]}-{chars[8..12]}-{chars[12..]}";
    }

    public static bool TryParse(string text, out byte[] idBytes)
    {
        idBytes = [];
        if (text is not { Length: 22 } || !text.StartsWith("GL-", StringComparison.Ordinal)
            || text[7] != '-' || text[12] != '-' || text[17] != '-')
        {
            return false;
        }

        UInt128 n = 0;
        foreach (char c in text.AsSpan(3))
        {
            if (c == '-')
            {
                continue;
            }

            int value = Alphabet.IndexOf(c, StringComparison.Ordinal);
            if (value < 0)
            {
                return false;
            }

            n = (n << 5) | (uint)value;
        }

        var bytes = new byte[ByteLength];
        for (int i = ByteLength - 1; i >= 0; i--)
        {
            bytes[i] = (byte)(n & 0xFF);
            n >>= 8;
        }

        idBytes = bytes;
        return true;
    }
}
