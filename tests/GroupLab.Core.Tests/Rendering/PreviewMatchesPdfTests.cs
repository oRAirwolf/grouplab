using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using SkiaSharp;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 250 section 1. The Targets screen's preview showed the lines and codes of a sheet and none of its words: no
/// legend, no labels, no check bar, no load block, no identifier. To a person that is a wrong picture of the sheet. The preview now draws the
/// words too, from the same scene the PDF is written from, and this holds every sheet's preview to its PDF, page by page, as a printer would
/// draw it: they may differ by antialiasing and the fine shape of a letter, and by nothing a person would see.
/// </summary>
public class PreviewMatchesPdfTests
{
    private const int Dpi = 150;

    /// <summary>A grey level this far from the other picture's, with nothing that close within <see cref="Reach"/> pixels, is a real difference.</summary>
    private const int Apart = 150;

    /// <summary>
    /// Three pixels: at 150 dpi the large labels of a zeroing grid show the fine difference between Liberation Sans and the reader's Helvetica
    /// along their edges, while a missing word is its whole width.
    /// </summary>
    private const int Reach = 3;

    public static IEnumerable<(string Name, TargetDefinition Definition)> Sheets()
    {
        foreach (var sheet in TargetLibrary.Load(Repo.PathTo("targets")))
        {
            yield return (sheet.File, sheet.Definition);
        }

        // A sheet from the designer (Made for your optic), which no library file holds.
        var made = TargetGenerator.Generate(new GeneratorRequest(100, 10, null, 25, ParametricSheet.Pages[0]));
        yield return ("Made for your optic, 100 yd at 10x", made.Design!.Definition!);
    }

    [Fact]
    public void EveryPreviewIsItsPdfPageWordsIncluded()
    {
        var wrong = new List<string>();
        var withoutWords = new List<string>();
        int pages = 0;
        foreach (var (name, definition) in Sheets())
        {
            var options = new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote);
            var rendered = TargetRenderer.Render(definition, options);
            Assert.True(rendered.Pdf is not null, name + " did not render");
            for (int page = 0; page < rendered.Pages.Count; page++)
            {
                pages++;
                var printed = Luminance(rendered.Pdf!, page);
                var drawn = SceneRasterizer.Rasterize(rendered.Pages[page], Dpi, words: true);
                int apart = Differences(drawn, printed);
                if (apart > Allowed(printed, rendered.Pages[page]))
                {
                    wrong.Add($"{name} page {page + 1}: {apart} pixels differ");
                }

                // The same check on the old preview, with no words: it has to fail, or it could not have caught what Alan saw.
                int blind = Differences(SceneRasterizer.Rasterize(rendered.Pages[page], Dpi), printed);
                if (blind <= Allowed(printed, rendered.Pages[page]))
                {
                    withoutWords.Add($"{name} page {page + 1}: {blind} pixels differ, {Allowed(printed, rendered.Pages[page])} allowed");
                }
            }
        }

        Assert.True(pages >= 23, $"only {pages} pages were compared");
        Assert.True(wrong.Count == 0, "the preview is not the sheet as it prints:\n  " + string.Join("\n  ", wrong));
        Assert.True(withoutWords.Count == 0, "without its words the preview still passed, so this check cannot see missing words:\n  " + string.Join("\n  ", withoutWords));
    }

    /// <summary>
    /// Section 1 item 3: until the zeroing grids are redrawn, no word on any sheet runs into another. Each run's box is its advance across,
    /// as the PDF sets it, and from its cap height above the baseline to its descenders below.
    /// </summary>
    [Fact]
    public void NoWordsOnAnySheetRunTogether()
    {
        var wrong = new List<string>();
        foreach (var (name, definition) in Sheets())
        {
            var pages = TargetRenderer.Render(definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote)).Pages;
            for (int page = 0; page < pages.Count; page++)
            {
                var boxes = pages[page].Items.OfType<TextRun>().Where(r => r.Text.Trim().Length > 0).Select(r =>
                {
                    long width = GroupLab.Core.Rendering.Pdf.HelveticaMetrics.TextWidth(r.Text, r.FontSize, r.Bold);
                    double left = r.Anchor switch { TextAnchor.Right => r.X - width, TextAnchor.Centre => r.X - (width / 2.0), _ => r.X };
                    // Only letters that hang below the line have a descender; a number sits on it.
                    double below = r.Text.IndexOfAny(['g', 'j', 'p', 'q', 'y', '(', ')', ',', ';', 'Q', 'J']) >= 0 ? 0.21 : 0.02;
                    return (r.Text, Left: left, Right: left + width, Top: r.Baseline - (0.72 * r.FontSize), Bottom: r.Baseline + (below * r.FontSize));
                }).ToList();
                for (int i = 0; i < boxes.Count; i++)
                {
                    for (int j = i + 1; j < boxes.Count; j++)
                    {
                        var (p, q) = (boxes[i], boxes[j]);
                        if (p.Left < q.Right && q.Left < p.Right && p.Top < q.Bottom && q.Top < p.Bottom)
                        {
                            wrong.Add($"{name} page {page + 1}: \"{p.Text}\" and \"{q.Text}\"");
                        }
                    }
                }
            }
        }

        Assert.True(wrong.Count == 0, "words that run into each other:\n  " + string.Join("\n  ", wrong.Distinct()));
    }

    /// <summary>
    /// A few pixels, and a small share of the page's lettering for the fine shape of a letter, which is Liberation Sans here and the reader's
    /// own Helvetica there (the foot of a large "1" is one). Measured 2026-09-28 at 150 dpi: every sheet's preview differed from its PDF by 0
    /// to 4 pixels but the zeroing grids, whose large labels differ by about 1,300; without its words the least a page differed was 1,643.
    /// </summary>
    private static int Allowed(GrayImage page, Scene scene)
    {
        double k = Dpi / 508.0;
        double lettering = scene.Items.OfType<TextRun>().Sum(r => GroupLab.Core.Rendering.Pdf.HelveticaMetrics.TextWidth(r.Text, r.FontSize, r.Bold) * k * r.FontSize * k);
        return 50 + (int)(0.03 * lettering);
    }

    /// <summary>Pixels of <paramref name="a"/> with no pixel within <see cref="Reach"/> of them in <paramref name="b"/> near its grey, and the same the other way.</summary>
    private static int Differences(GrayImage a, GrayImage b)
    {
        int width = Math.Min(a.Width, b.Width), height = Math.Min(a.Height, b.Height);
        return OneWay(a, b, width, height) + OneWay(b, a, width, height);
    }

    private static int OneWay(GrayImage a, GrayImage b, int width, int height)
    {
        // The darkest and lightest of b within Reach of each pixel, by two passes each way: a pixel of a outside that range by Apart has
        // nothing near it in b.
        var low = Spread(b, width, height, Math.Min);
        var high = Spread(b, width, height, Math.Max);
        int count = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int value = a.Pixels[(y * a.Width) + x], i = (y * width) + x;
                if (value < low[i] - Apart || value > high[i] + Apart)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static byte[] Spread(GrayImage image, int width, int height, Func<byte, byte, byte> pick)
    {
        var across = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte v = image.Pixels[(y * image.Width) + x];
                for (int d = Math.Max(0, x - Reach); d <= Math.Min(width - 1, x + Reach); d++)
                {
                    v = pick(v, image.Pixels[(y * image.Width) + d]);
                }

                across[(y * width) + x] = v;
            }
        }

        var both = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte v = across[(y * width) + x];
                for (int d = Math.Max(0, y - Reach); d <= Math.Min(height - 1, y + Reach); d++)
                {
                    v = pick(v, across[(d * width) + x]);
                }

                both[(y * width) + x] = v;
            }
        }

        return both;
    }

    /// <summary>A PDF page drawn by PDFium, as the rasteriser weighs colour: luminance, so a coloured ink compares with its grey.</summary>
    private static GrayImage Luminance(byte[] pdf, int page)
    {
        using var bitmap = PDFtoImage.Conversion.ToImage(pdf, page, options: new PDFtoImage.RenderOptions { Dpi = Dpi, BackgroundColor = SKColors.White });
        var grey = new byte[bitmap.Width * bitmap.Height];
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var c = bitmap.GetPixel(x, y);
                grey[(y * bitmap.Width) + x] = (byte)Math.Clamp(Math.Round((0.299 * c.Red) + (0.587 * c.Green) + (0.114 * c.Blue)), 0, 255);
            }
        }

        return new GrayImage(bitmap.Width, bitmap.Height, grey);
    }
}
