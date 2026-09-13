namespace GroupLab.Core.Rendering.Pdf;

/// <summary>
/// Glyph advance widths of Helvetica, one of the fourteen standard PDF fonts, which every conforming reader supplies,
/// so the renderer embeds no font file. Widths are the Adobe font metrics in thousandths of the font size, for the
/// printable ASCII range; text is written in WinAnsiEncoding.
/// </summary>
public static class HelveticaMetrics
{
    /// <summary>Height of a capital letter, in thousandths of the font size.</summary>
    public const int CapHeight = 718;

    /// <summary>Advance used for a Latin-1 character outside the table, whose exact width does not affect geometry.</summary>
    public const int FallbackWidth = 556;

    private static readonly short[] PrintableAsciiWidths =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584,
    ];

    /// <summary>
    /// The character as written into the PDF: printable ASCII and Latin-1 pass through, anything else becomes a
    /// question mark, because WinAnsiEncoding cannot represent it.
    /// </summary>
    public static char ToWinAnsi(char c) => c is (>= ' ' and <= '~') or (>= ' ' and <= 'ÿ') ? c : '?';

    public static int Width(char c)
    {
        char mapped = ToWinAnsi(c);
        return mapped is >= ' ' and <= '~' ? PrintableAsciiWidths[mapped - ' '] : FallbackWidth;
    }

    /// <summary>The advance of <paramref name="text"/> at <paramref name="fontSize"/>, rounded up to a whole unit.</summary>
    public static long TextWidth(string text, long fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        long units = 0;
        foreach (char c in text)
        {
            units += Width(c);
        }

        return ((units * fontSize) + 999) / 1000;
    }

    /// <summary>The largest font size, at most <paramref name="maximum"/>, at which <paramref name="text"/> fits <paramref name="width"/>.</summary>
    public static long FitFontSize(string text, long width, long maximum)
    {
        ArgumentNullException.ThrowIfNull(text);
        long units = text.Sum(c => (long)Width(c));
        return units == 0 ? maximum : Math.Min(maximum, width * 1000 / units);
    }
}
