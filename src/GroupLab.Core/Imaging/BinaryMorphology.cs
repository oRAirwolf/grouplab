using System.Numerics;

namespace GroupLab.Core.Imaging;

/// <summary>
/// A morphological close of a 0 and 1 image, exactly as OpenCV's <c>morphologyEx</c> computes it with the same structuring element and its
/// default border, in a fraction of the time. NOTES-FROM-PLANNING.md entry 392 section 2: the close at the end of hole finding, a disc 67
/// pixels across on a 600 dpi scan, was 1.7 of the 2.6 seconds a scan took, because the native filter takes the maximum over every point of
/// the disc at every pixel.
/// <para>
/// <b>Why it is the same answer, not a close one.</b> A structuring element whose rows are each one centred run of points, as OpenCV's
/// ellipse is, reaches a pixel from a row <c>dy</c> away exactly when that row has a set pixel within the run's half width. So a dilation
/// is: for each row, the horizontal distance from every pixel to the nearest set pixel, and then, for each pixel, whether any row of the
/// element finds one within its half width. An erosion is the same question asked of the nearest unset pixel. Beyond the image counts as
/// neither, which is OpenCV's default border for both. Everything is integer comparison, so it is the same on every platform; the
/// half widths are read from the element the native code would have used, never recomputed.
/// </para>
/// </summary>
public static class BinaryMorphology
{
    /// <summary>The largest half width a row distance can be compared with: distances are held in a byte, and 255 means none in reach.</summary>
    public const int WidestHalfWidth = 254;

    private const byte None = 255;

    /// <summary>Whether every pixel is 0 or 1, the only images this answers for.</summary>
    public static bool IsBinary(byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        var span = pixels.AsSpan();
        var one = Vector<byte>.One;
        int i = 0;
        for (; i <= span.Length - Vector<byte>.Count; i += Vector<byte>.Count)
        {
            if (Vector.GreaterThanAny(new Vector<byte>(span.Slice(i, Vector<byte>.Count)), one))
            {
                return false;
            }
        }

        for (; i < span.Length; i++)
        {
            if (span[i] > 1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The half width of each row of a structuring element, top to bottom, where every row is one run of points centred on the middle
    /// column, or null where the element is not of that shape or is wider than <see cref="WidestHalfWidth"/>.
    /// </summary>
    /// <param name="element">The element, row-major, nonzero where it has a point.</param>
    public static int[]? HalfWidths(byte[] element, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (width % 2 == 0 || height % 2 == 0 || element.Length != width * height || width / 2 > WidestHalfWidth)
        {
            return null;
        }

        int centre = width / 2;
        var halves = new int[height];
        for (int y = 0; y < height; y++)
        {
            int first = -1, last = -1;
            for (int x = 0; x < width; x++)
            {
                if (element[(y * width) + x] == 0)
                {
                    continue;
                }

                if (first < 0)
                {
                    first = x;
                }
                else if (x != last + 1)
                {
                    return null;
                }

                last = x;
            }

            if (first < 0 || centre - first != last - centre)
            {
                return null;
            }

            halves[y] = centre - first;
        }

        return halves;
    }

    /// <summary>The close, a dilation then an erosion by the same element, of an image whose pixels are all 0 or 1.</summary>
    /// <param name="halfWidths">From <see cref="HalfWidths"/>: one per row of an element of odd height, centred on the pixel.</param>
    public static byte[] Close(byte[] pixels, int width, int height, int[] halfWidths)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ArgumentNullException.ThrowIfNull(halfWidths);
        if (halfWidths.Length % 2 == 0 || halfWidths.Any(w => w is < 0 or > WidestHalfWidth))
        {
            throw new ArgumentException("one half width per row of an element of odd height, each 0 to " + WidestHalfWidth, nameof(halfWidths));
        }

        var dilated = Pass(pixels, width, height, halfWidths, dilate: true);
        return Pass(dilated, width, height, halfWidths, dilate: false);
    }

    /// <summary>
    /// One dilation or erosion. A dilation sets a pixel where some row of the element finds a set pixel within its half width; an erosion
    /// keeps a pixel set only where no row of the element finds an unset one. Rows beyond the image are skipped, as OpenCV's default border is.
    /// </summary>
    private static byte[] Pass(byte[] source, int width, int height, int[] halfWidths, bool dilate)
    {
        byte sought = dilate ? (byte)1 : (byte)0;
        int reach = halfWidths.Length / 2;
        var distance = new byte[source.Length];
        var hasSought = new bool[height];
        Parallel.For(0, height, y => hasSought[y] = RowDistance(source.AsSpan(y * width, width), distance.AsSpan(y * width, width), sought));

        // How many rows up to each one hold what is sought, so a row with none of it within reach is decided at once.
        var before = new int[height + 1];
        for (int y = 0; y < height; y++)
        {
            before[y + 1] = before[y] + (hasSought[y] ? 1 : 0);
        }

        var result = new byte[source.Length];
        Parallel.For(0, height, y =>
        {
            int top = Math.Max(0, y - reach), bottom = Math.Min(height - 1, y + reach);
            var row = result.AsSpan(y * width, width);
            bool anyInReach = before[bottom + 1] - before[top] > 0;
            if (dilate)
            {
                if (!anyInReach)
                {
                    return;
                }
            }
            else
            {
                // An erosion keeps nothing in a row that is unset itself, and keeps all of a row with no unset pixel anywhere in reach.
                if (hasSought[y] && source.AsSpan(y * width, width).IndexOfAnyExcept((byte)0) < 0)
                {
                    return;
                }

                row.Fill(1);
                if (!anyInReach)
                {
                    return;
                }
            }

            for (int other = top; other <= bottom; other++)
            {
                if (!hasSought[other])
                {
                    continue;
                }

                Combine(row, distance.AsSpan(other * width, width), (byte)halfWidths[other - y + reach], dilate);
            }
        });

        return result;
    }

    /// <summary>
    /// Each pixel's distance along its row to the nearest pixel of the value sought, <see cref="None"/> where there is none within
    /// <see cref="WidestHalfWidth"/>; whether the row holds the value at all.
    /// </summary>
    private static bool RowDistance(ReadOnlySpan<byte> row, Span<byte> distance, byte sought)
    {
        if (row.IndexOf(sought) < 0)
        {
            distance.Fill(None);
            return false;
        }

        byte d = None;
        for (int x = 0; x < row.Length; x++)
        {
            d = row[x] == sought ? (byte)0 : d < None ? (byte)(d + 1) : None;
            distance[x] = d;
        }

        d = None;
        for (int x = row.Length - 1; x >= 0; x--)
        {
            d = row[x] == sought ? (byte)0 : d < None ? (byte)(d + 1) : None;
            if (d < distance[x])
            {
                distance[x] = d;
            }
        }

        return true;
    }

    /// <summary>A dilation sets where the other row's set pixel is within reach; an erosion clears where its unset pixel is.</summary>
    private static void Combine(Span<byte> row, ReadOnlySpan<byte> distance, byte halfWidth, bool dilate)
    {
        var reach = new Vector<byte>(halfWidth);
        var one = Vector<byte>.One;
        int x = 0;
        for (; x <= row.Length - Vector<byte>.Count; x += Vector<byte>.Count)
        {
            var within = Vector.LessThanOrEqual(new Vector<byte>(distance.Slice(x, Vector<byte>.Count)), reach) & one;
            var here = new Vector<byte>(row.Slice(x, Vector<byte>.Count));
            (dilate ? here | within : here & ~within & one).CopyTo(row.Slice(x, Vector<byte>.Count));
        }

        for (; x < row.Length; x++)
        {
            bool near = distance[x] <= halfWidth;
            row[x] = dilate ? (byte)(row[x] | (near ? 1 : 0)) : (byte)(near ? 0 : row[x]);
        }
    }
}
