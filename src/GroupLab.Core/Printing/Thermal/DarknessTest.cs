namespace GroupLab.Core.Printing.Thermal;

/// <summary>
/// The darkness test of NOTES-FROM-PLANNING.md entry 358 section 6: one strip per setting, printed one under another, each with the setting's
/// number, a solid black square, lines 1 to 4 dots wide and squares 3 to 6 dots wide, so a photograph of the print shows what each setting does
/// to fine detail. <see cref="Recommend"/> takes the line widths measured on that photograph and picks the darkest setting that grows the fine
/// features by no more than half a dot. Finding each command's real range and direction, and reading the photograph, wait on the test
/// printers (requests 72 and 73); the pattern and the rule do not.
/// </summary>
public static class DarknessTest
{
    /// <summary>The line widths drawn in each strip, dots.</summary>
    public static IReadOnlyList<int> LineDots { get; } = [1, 2, 3, 4];

    /// <summary>The square sizes drawn in each strip, dots: the sizes a tag's module comes to on a thermal head.</summary>
    public static IReadOnlyList<int> SquareDots { get; } = [3, 4, 5, 6];

    /// <summary>The most a fine feature may grow at the setting recommended, dots.</summary>
    public const double MostGrowth = 0.5;

    private const int Strip = 64;

    /// <summary>
    /// The test as one page of dots <paramref name="widthDots"/> across: a strip 64 dots deep for each setting, in the order given, each
    /// starting with its number drawn in a 5 by 7 dot font at three dots a pixel.
    /// </summary>
    public static DotImage Page(int widthDots, IReadOnlyList<int> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Count == 0)
        {
            throw new ArgumentException("no settings to test", nameof(settings));
        }

        var page = new DotImage(widthDots, (Strip * settings.Count) + 8);
        for (int i = 0; i < settings.Count; i++)
        {
            DrawStrip(page, (i * Strip) + 8, settings[i]);
        }

        return page;
    }

    /// <summary>
    /// A whole page <paramref name="widthDots"/> by <paramref name="heightDots"/> with <paramref name="strips"/> strips spread down it,
    /// numbered by place from the top, for printing through a printer's own app once at each darkness it offers.
    /// </summary>
    public static DotImage Sheet(int widthDots, int heightDots, int strips)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(strips);
        var page = new DotImage(widthDots, heightDots);
        int gap = Math.Max(Strip, (heightDots - Strip) / Math.Max(1, strips));
        for (int i = 0; i < strips; i++)
        {
            int top = Math.Min(heightDots - Strip, (Strip / 2) + (i * gap));
            DrawStrip(page, top, i + 1);
        }

        return page;
    }

    private static void DrawStrip(DotImage page, int top, int label)
    {
        int x = Number(page, label, 4, top + 8, 3);
        x += 12;
        page.Fill(x, top + 4, x + 40, top + 44);
        x += 52;
        foreach (int width in LineDots)
        {
            page.Fill(x, top + 4, x + width, top + 44);
            x += width + 12;
        }

        foreach (int side in SquareDots)
        {
            page.Fill(x, top + 24 - (side / 2), x + side, top + 24 - (side / 2) + side);
            x += side + 12;
        }
    }

    /// <summary>
    /// The darkest setting whose measured lines are each no more than <see cref="MostGrowth"/> dots wider than drawn, or null where none is.
    /// <paramref name="measured"/> gives, for each setting, the widths of its 1 to 4 dot lines as the photograph shows them, in dots.
    /// "Darkest" follows <paramref name="higherIsDarker"/>, since printers number darkness both ways.
    /// </summary>
    public static int? Recommend(IReadOnlyDictionary<int, IReadOnlyList<double>> measured, bool higherIsDarker = true)
    {
        ArgumentNullException.ThrowIfNull(measured);
        var keeping = measured
            .Where(m => m.Value.Count == LineDots.Count && m.Value.Select((w, i) => w - LineDots[i]).All(g => g <= MostGrowth))
            .Select(m => m.Key)
            .ToList();
        return keeping.Count == 0 ? null : higherIsDarker ? keeping.Max() : keeping.Min();
    }

    /// <summary>Draws a setting's number with its left edge at <paramref name="x"/>; returns where the next thing may start.</summary>
    private static int Number(DotImage page, int value, int x, int y, int scale)
    {
        foreach (char c in value.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            if (c == '-')
            {
                page.Fill(x, y + (3 * scale), x + (5 * scale), y + (4 * scale));
            }
            else
            {
                var rows = Digits[c - '0'];
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if ((rows[row] & (0x10 >> col)) != 0)
                        {
                            page.Fill(x + (col * scale), y + (row * scale), x + ((col + 1) * scale), y + ((row + 1) * scale));
                        }
                    }
                }
            }

            x += 6 * scale;
        }

        return x;
    }

    /// <summary>The ten digits in a 5 by 7 dot font, a row to a number, the leftmost dot in the 0x10 bit.</summary>
    private static readonly byte[][] Digits =
    [
        [0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E],
        [0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E],
        [0x0E, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1F],
        [0x1F, 0x02, 0x04, 0x02, 0x01, 0x11, 0x0E],
        [0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02],
        [0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E],
        [0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E],
        [0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08],
        [0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E],
        [0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C],
    ];
}
