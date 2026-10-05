namespace GroupLab.Core.Printing.Labels;

/// <summary>
/// LZO1X, the compression the Phomemo M834 takes its raster in (request 73's recording, 2026-10-05: each 4096 bytes of the page arrive as
/// one LZO1X block). Written here from the format as Markus Oberhumer's LZO and the Linux kernel's decompressor describe it; no LZO code is
/// bundled. The compressor is a plain greedy one that uses only literal runs and the M3 match (any length, up to 16384 back), which every
/// LZO1X decompressor reads; it gives up a little size for being short enough to check by eye.
/// </summary>
public static class Lzo1x
{
    private const int MaxDistance = 16384;

    public static byte[] Compress(ReadOnlySpan<byte> source)
    {
        var output = new List<byte>((source.Length / 8) + 16);
        var head = new int[1 << 12];
        Array.Fill(head, -1);
        int literalsFrom = 0, i = 0, patch = -1;
        while (i + 3 <= source.Length)
        {
            int h = ((source[i] << 7) ^ (source[i + 1] << 4) ^ source[i + 2]) & 0xFFF;
            int candidate = head[h];
            head[h] = i;
            int length = 0;
            if (candidate >= 0 && i - candidate <= MaxDistance && source[candidate] == source[i] && source[candidate + 1] == source[i + 1] && source[candidate + 2] == source[i + 2])
            {
                length = 3;
                while (i + length < source.Length && source[candidate + length] == source[i + length])
                {
                    length++;
                }
            }

            if (length < 3)
            {
                i++;
                continue;
            }

            Literals(output, source[literalsFrom..i], ref patch);
            Match(output, i - candidate, length, ref patch);
            i += length;
            literalsFrom = i;
        }

        Literals(output, source[literalsFrom..], ref patch);
        output.AddRange([0x11, 0x00, 0x00]);
        return [.. output];
    }

    /// <summary>A run of literals: the first in the stream says its length in its own byte; up to three after a match ride in its low bits.</summary>
    private static void Literals(List<byte> output, ReadOnlySpan<byte> run, ref int patch)
    {
        int n = run.Length;
        if (n == 0)
        {
            return;
        }

        if (output.Count == 0 && n <= 238)
        {
            output.Add((byte)(17 + n));
        }
        else if (patch >= 0 && n <= 3)
        {
            output[patch] |= (byte)n;
        }
        else
        {
            Length(output, n - 3, 15, 0x00);
        }

        foreach (byte b in run)
        {
            output.Add(b);
        }

        patch = -1;
    }

    /// <summary>The M3 match: 001LLLLL, its length beyond 2 (longer ones run on in zero bytes), then the distance less one, times four, low byte first.</summary>
    private static void Match(List<byte> output, int distance, int length, ref int patch)
    {
        Length(output, length - 2, 31, 0x20);
        int d = (distance - 1) << 2;
        patch = output.Count;
        output.Add((byte)(d & 0xFF));
        output.Add((byte)(d >> 8));
    }

    /// <summary>A length in LZO's way: in the instruction byte if it fits, otherwise zero there, a zero byte for each 255, and the rest.</summary>
    private static void Length(List<byte> output, int value, int fits, byte instruction)
    {
        if (value <= fits)
        {
            output.Add((byte)(instruction | value));
            return;
        }

        output.Add(instruction);
        int rest = value - fits;
        while (rest > 255)
        {
            output.Add(0);
            rest -= 255;
        }

        output.Add((byte)rest);
    }

    /// <summary>Unpacks one LZO1X block, as the recording reader and the tests need; it stops at the end marker and refuses anything malformed.</summary>
    public static byte[] Decompress(ReadOnlySpan<byte> block)
    {
        byte[] source = block.ToArray();
        var output = new List<byte>(source.Length * 8);
        int ip = 0, state = 0;
        byte Next() => ip < source.Length ? source[ip++] : throw new InvalidDataException("the LZO block ends early");
        void Take(int n, ReadOnlySpan<byte> s)
        {
            if (ip + n > s.Length)
            {
                throw new InvalidDataException("the LZO block ends early");
            }

            for (int k = 0; k < n; k++)
            {
                output.Add(s[ip + k]);
            }

            ip += n;
        }

        void Copy(int from, int n)
        {
            if (from < 0)
            {
                throw new InvalidDataException("an LZO match reaches before the block's start");
            }

            for (int k = 0; k < n; k++)
            {
                output.Add(output[from + k]);
            }
        }

        int Extended(int fits)
        {
            int n = 0;
            byte b;
            while ((b = Next()) == 0)
            {
                n += 255;
            }

            return n + fits + b;
        }

        if (source.Length > 0 && source[0] > 17)
        {
            int t = Next() - 17;
            Take(t, source);
            state = t < 4 ? t : 4;
        }

        while (true)
        {
            int t = Next(), next, from, n;
            if (t < 16)
            {
                if (state == 0)
                {
                    Take((t == 0 ? Extended(15) : t) + 3, source);
                    state = 4;
                    continue;
                }

                from = output.Count - 1 - (t >> 2) - (Next() << 2) - (state == 4 ? MaxDistance / 8 : 0);
                n = state == 4 ? 3 : 2;
                next = t & 3;
            }
            else if (t >= 64)
            {
                from = output.Count - 1 - ((t >> 2) & 7) - (Next() << 3);
                n = (t >> 5) + 1;
                next = t & 3;
            }
            else if (t >= 32)
            {
                n = ((t & 31) == 0 ? Extended(31) : t & 31) + 2;
                int d = Next() | (Next() << 8);
                from = output.Count - 1 - (d >> 2);
                next = d & 3;
            }
            else
            {
                n = ((t & 7) == 0 ? Extended(7) : t & 7) + 2;
                int d = Next() | (Next() << 8);
                from = output.Count - ((t & 8) << 11) - (d >> 2);
                if (from == output.Count)
                {
                    return [.. output];
                }

                from -= 0x4000;
                next = d & 3;
            }

            Copy(from, n);
            Take(next, source);
            state = next;
        }
    }
}
