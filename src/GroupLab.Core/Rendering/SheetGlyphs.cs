using System.Globalization;
using GroupLab.Core.Rendering.Pdf;

namespace GroupLab.Core.Rendering;

/// <summary>
/// The outlines a sheet's words are drawn with when GroupLab draws the sheet itself, NOTES-FROM-PLANNING.md entry 250 section 1. The PDF
/// names Helvetica and the reader supplies it; the preview on the Targets screen has no reader, so it showed the artwork and none of the
/// writing. Liberation Sans is metric-compatible with Helvetica and free to redistribute, and its outlines, written by
/// <c>scripts/sheet-glyphs.py</c> into an embedded file with the font's license at its head, are placed at Helvetica's own advances, so each word sits where the PDF sets it.
/// </summary>
public static class SheetGlyphs
{
    private const double UnitsPerEm = 2048;

    /// <summary>Straight pieces a quadratic curve is drawn with: at the sizes a sheet uses, finer than a pixel.</summary>
    private const int CurvePieces = 8;

    private static readonly Lazy<Dictionary<char, IReadOnlyList<(double X, double Y)[]>>> Outlines = new(Load);

    /// <summary>
    /// The closed outlines of <paramref name="text"/> set as <see cref="PdfWriter"/> sets a <see cref="TextRun"/>, in the page's own units,
    /// y down. Filled with the nonzero rule they are the words.
    /// </summary>
    public static IEnumerable<(double X, double Y)[]> Contours(TextRun text)
    {
        ArgumentNullException.ThrowIfNull(text);
        long width = HelveticaMetrics.TextWidth(text.Text, text.FontSize);
        double x = text.Anchor switch
        {
            TextAnchor.Right => text.X - width,
            TextAnchor.Centre => text.X - (width / 2.0),
            _ => text.X,
        };
        double k = text.FontSize / UnitsPerEm;
        foreach (char c in text.Text)
        {
            char mapped = HelveticaMetrics.ToWinAnsi(c);
            if (Outlines.Value.TryGetValue(mapped, out var contours))
            {
                foreach (var contour in contours)
                {
                    var placed = new (double X, double Y)[contour.Length];
                    for (int i = 0; i < contour.Length; i++)
                    {
                        placed[i] = (x + (contour[i].X * k), text.Baseline - (contour[i].Y * k));
                    }

                    yield return placed;
                }
            }

            x += HelveticaMetrics.Width(c) * text.FontSize / 1000.0;
        }
    }

    private static Dictionary<char, IReadOnlyList<(double X, double Y)[]>> Load()
    {
        using var stream = typeof(SheetGlyphs).Assembly.GetManifestResourceStream("GroupLab.Core.Rendering.SheetSans.glyphs")
            ?? throw new InvalidOperationException("the sheet glyphs are not embedded");
        using var reader = new StreamReader(stream);
        var glyphs = new Dictionary<char, IReadOnlyList<(double X, double Y)[]>>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var contours = new List<(double X, double Y)[]>();
            var current = new List<(double X, double Y)>();
            (double X, double Y) at = default;
            double N(int i) => double.Parse(parts[i], CultureInfo.InvariantCulture);
            for (int i = 1; i < parts.Length;)
            {
                switch (parts[i])
                {
                    case "M":
                        at = (N(i + 1), N(i + 2));
                        current = [at];
                        i += 3;
                        break;
                    case "L":
                        at = (N(i + 1), N(i + 2));
                        current.Add(at);
                        i += 3;
                        break;
                    case "Q":
                        var control = (X: N(i + 1), Y: N(i + 2));
                        var end = (X: N(i + 3), Y: N(i + 4));
                        for (int s = 1; s <= CurvePieces; s++)
                        {
                            double t = s / (double)CurvePieces, u = 1 - t;
                            current.Add(((u * u * at.X) + (2 * u * t * control.X) + (t * t * end.X), (u * u * at.Y) + (2 * u * t * control.Y) + (t * t * end.Y)));
                        }

                        at = end;
                        i += 5;
                        break;
                    default:
                        if (current.Count > 2)
                        {
                            contours.Add([.. current]);
                        }

                        current = [];
                        i++;
                        break;
                }
            }

            glyphs[(char)int.Parse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture)] = contours;
        }

        return glyphs;
    }
}
