using System.Numerics;

namespace GroupLab.Core.Imaging;

/// <summary>
/// A morphological open or close of a grey image, exactly as OpenCV's <c>morphologyEx</c> computes it with the same structuring element and
/// its default border, in a fraction of the time. NOTES-FROM-PLANNING.md entry 484 section 2: finding holes on a target GroupLab did not
/// print spent more than half its time in OpenCV's grey open and close by discs up to 121 pixels across, at about 100 dots an inch, because
/// the native filter takes the maximum over every point of the disc at every pixel.
/// <para>
/// <b>Why it is the same answer, not a close one.</b> A grey dilation by a flat element is, at each pixel, the largest value under the
/// element; an erosion the smallest. When every row of the element is one run of points centred on the middle column, as OpenCV's ellipse
/// is (<see cref="BinaryMorphology.HalfWidths"/>), that is the largest over the element's rows of each row's largest value within the run's
/// half width. A row's largest within <c>w + 1</c> is its largest within <c>w</c> and the two pixels one step further out, so every half
/// width of a row is had in one pass each. Beyond the image counts as neither, which is OpenCV's default border for both, and is the rule
/// <see cref="BinaryMorphology"/> is already held to. Only largest and smallest of bytes are taken, so it is the same on every platform.
/// </para>
/// </summary>
public static class GreyMorphology
{
    /// <summary>The close, a dilation then an erosion by the same element.</summary>
    /// <param name="halfWidths">From <see cref="BinaryMorphology.HalfWidths"/>: one per row of an element of odd height, centred on the pixel.</param>
    public static byte[] Close(byte[] pixels, int width, int height, int[] halfWidths) =>
        Pass(Pass(pixels, width, height, halfWidths, dilate: true), width, height, halfWidths, dilate: false);

    /// <summary>The open, an erosion then a dilation by the same element.</summary>
    /// <param name="halfWidths">From <see cref="BinaryMorphology.HalfWidths"/>: one per row of an element of odd height, centred on the pixel.</param>
    public static byte[] Open(byte[] pixels, int width, int height, int[] halfWidths) =>
        Pass(Pass(pixels, width, height, halfWidths, dilate: false), width, height, halfWidths, dilate: true);

    /// <summary>
    /// One dilation or erosion, worked in bands of rows at once. Each band reads the rows within the element's reach of it; for each, the
    /// row's largest (or smallest) within every half width the element has, widened one step at a time, is combined into the band's rows
    /// that the element reaches it from with that half width. Rows beyond the image are skipped.
    /// </summary>
    private static byte[] Pass(byte[] source, int width, int height, int[] halfWidths, bool dilate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(halfWidths);
        if (source.Length != width * height || halfWidths.Length % 2 == 0)
        {
            throw new ArgumentException("the picture and its size, or the element's rows, do not agree");
        }

        int reach = halfWidths.Length / 2;
        int widest = halfWidths.Max();

        // The rows of the element with each half width, as their offsets from the pixel's row.
        var offsets = new List<int>[widest + 1];
        for (int i = 0; i < halfWidths.Length; i++)
        {
            (offsets[halfWidths[i]] ??= []).Add(i - reach);
        }

        var result = new byte[source.Length];
        result.AsSpan().Fill(dilate ? byte.MinValue : byte.MaxValue);
        int band = Math.Max(16, Math.Min(256, (height + Environment.ProcessorCount - 1) / Math.Max(1, Environment.ProcessorCount)));
        int bands = (height + band - 1) / band;
        Parallel.For(0, bands, b =>
        {
            int top = b * band, bottom = Math.Min(height, top + band);
            var widened = new byte[width];
            for (int other = Math.Max(0, top - reach); other < Math.Min(height, bottom + reach); other++)
            {
                var row = source.AsSpan(other * width, width);
                row.CopyTo(widened);
                for (int w = 0; w <= widest; w++)
                {
                    if (w > 0)
                    {
                        // Within w: within w - 1, and the pixels w to the left and w to the right where the picture has them.
                        if (w < width)
                        {
                            Combine(widened.AsSpan(w), row[..(width - w)], dilate);
                            Combine(widened.AsSpan(0, width - w), row[w..], dilate);
                        }
                    }

                    if (offsets[w] is not { } rows)
                    {
                        continue;
                    }

                    foreach (int dy in rows)
                    {
                        // The element's row dy below the pixel reaches this row from the pixel's row other - dy.
                        int y = other - dy;
                        if (y >= top && y < bottom)
                        {
                            Combine(result.AsSpan(y * width, width), widened, dilate);
                        }
                    }
                }
            }
        });
        return result;
    }

    /// <summary>Each byte of <paramref name="into"/> becomes the larger (or smaller) of itself and the same byte of <paramref name="with"/>.</summary>
    private static void Combine(Span<byte> into, ReadOnlySpan<byte> with, bool larger)
    {
        int i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            int step = Vector<byte>.Count;
            for (; i + step <= into.Length; i += step)
            {
                var a = new Vector<byte>(into[i..]);
                var c = new Vector<byte>(with[i..]);
                (larger ? Vector.Max(a, c) : Vector.Min(a, c)).CopyTo(into[i..]);
            }
        }

        for (; i < into.Length; i++)
        {
            into[i] = larger ? Math.Max(into[i], with[i]) : Math.Min(into[i], with[i]);
        }
    }
}
