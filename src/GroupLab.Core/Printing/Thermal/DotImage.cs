using GroupLab.Core.Imaging;

namespace GroupLab.Core.Printing.Thermal;

/// <summary>
/// One label as a thermal printer's head makes it, NOTES-FROM-PLANNING.md entry 358 section 2: a dot is black or it is not. Rows are packed
/// eight dots to a byte, the leftmost dot in the highest bit, and a set bit is a black dot, which is how the Phomemo family's raster, ZPL's
/// graphic field and ESC/POS's raster all take it; TSPL takes the opposite, and its encoder turns it over.
/// </summary>
public sealed class DotImage
{
    public DotImage(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        RowBytes = (width + 7) / 8;
        Bits = new byte[RowBytes * height];
    }

    /// <summary>Dots across, along the print head.</summary>
    public int Width { get; }

    /// <summary>Dots along the feed.</summary>
    public int Height { get; }

    /// <summary>Bytes in one row, the width rounded up to whole bytes; the bits past the width are always clear.</summary>
    public int RowBytes { get; }

    /// <summary>The rows, top first, packed as the summary says.</summary>
    public byte[] Bits { get; }

    public bool this[int x, int y]
    {
        get => (Bits[(y * RowBytes) + (x >> 3)] & (0x80 >> (x & 7))) != 0;
        set
        {
            int i = (y * RowBytes) + (x >> 3);
            Bits[i] = value ? (byte)(Bits[i] | (0x80 >> (x & 7))) : (byte)(Bits[i] & ~(0x80 >> (x & 7)));
        }
    }

    /// <summary>Blackens the dots from (<paramref name="x0"/>, <paramref name="y0"/>) up to, not including, (<paramref name="x1"/>, <paramref name="y1"/>), clipped to the image.</summary>
    public void Fill(long x0, long y0, long x1, long y1)
    {
        int left = (int)Math.Clamp(x0, 0, Width), right = (int)Math.Clamp(x1, 0, Width);
        int top = (int)Math.Clamp(y0, 0, Height), bottom = (int)Math.Clamp(y1, 0, Height);
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                this[x, y] = true;
            }
        }
    }

    /// <summary>How many dots are black, which a test and the darkness check count.</summary>
    public long BlackDots()
    {
        long count = 0;
        foreach (byte b in Bits)
        {
            count += System.Numerics.BitOperations.PopCount(b);
        }

        return count;
    }

    /// <summary>The image as grey pixels, one to a dot, black 0 and paper 255: the print preview, and a simulated print's starting point.</summary>
    public GrayImage ToGray()
    {
        var pixels = new byte[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                pixels[(y * Width) + x] = this[x, y] ? (byte)0 : (byte)255;
            }
        }

        return new GrayImage(Width, Height, pixels);
    }

    /// <summary>The rows with every bit turned over, for a printer that takes a set bit as paper (TSPL's BITMAP).</summary>
    public byte[] Inverted()
    {
        var bytes = new byte[Bits.Length];
        int spare = (RowBytes * 8) - Width;
        byte keep = (byte)(0xFF << spare);
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)~Bits[i];
            if (spare > 0 && (i % RowBytes) == RowBytes - 1)
            {
                // The bits past the width stay paper whichever way round: set, since a set bit is paper here.
                bytes[i] = (byte)(bytes[i] | (byte)~keep);
            }
        }

        return bytes;
    }
}
