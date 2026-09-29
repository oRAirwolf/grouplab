using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using OpenCvSharp;

namespace GroupLab.Cli;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 278 section 3, question 67 tested rather than chosen: three Letter check pages, identical but for the card
/// outline, each marked TEST A, B or C and "not for use", for Alan to print, scan and photograph with a card. A is the outline as built,
/// 3 mm outside the card; B a hairline on the card's edge; C corner marks only, 3 mm outside. The pages are written to a local folder and
/// never committed; the library's page stays A until the photographs decide.
/// </summary>
public static class ScaleTestVerb
{
    /// <summary>B's line: a hairline, 0.2 mm, centered on the card's edge.</summary>
    public const long HairlineDmm = 2;

    /// <summary>C's corner marks: arms 10 mm long along each side, at the built outline's weight and 3 mm gap.</summary>
    public const long CornerArmDmm = 100;

    public static int Run(string outDir, bool preview, TextWriter output, TextWriter error)
    {
        string path = Path.Combine("targets", "GL-SCALE-LTR-1.gltd.json");
        if (!File.Exists(path))
        {
            error.WriteLine($"{path} not found; run this from the repository's root.");
            return 2;
        }

        var definition = GltdJsonReader.Read(File.ReadAllBytes(path)).Definition ?? throw new InvalidDataException(path + " did not read as a target definition.");
        var page = SceneBuilder.Build(definition).Pages[0];
        Directory.CreateDirectory(outDir);
        foreach (char variant in "ABC")
        {
            string file = Path.Combine(outDir, $"TEST-{variant}.pdf");
            var scene = Variant(page, definition.Page, variant);
            File.WriteAllBytes(file, PdfWriter.Write([scene]));
            if (preview)
            {
                // A look at the page before printing it, at 100 dpi with its words, beside the PDF.
                var image = SceneRasterizer.Rasterize(scene, 100, words: true);
                using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
                Cv2.ImWrite(Path.ChangeExtension(file, ".png"), mat);
            }

            output.WriteLine($"{file}: {Words(variant)}");
        }

        return 0;
    }

    public static string Words(char variant) => variant switch
    {
        'A' => "the outline 3 mm outside the card, as built",
        'B' => "a hairline on the card's edge",
        _ => "corner marks only, 3 mm outside the card",
    };

    /// <summary>The built page with its card outline replaced by the variant's, its note changed to match, and the test's name in large type.</summary>
    public static Scene Variant(Scene built, Core.Gltd.Model.Page size, char variant)
    {
        var card = GridStyle4.Card(size);
        long o = GridStyle4.OutlineStroke, gap = GridStyle4.OutlineGap, w = GridStyle4.CardWidthDmm, h = GridStyle4.CardHeightDmm;
        var outline = new[]
        {
            (card.X - gap - o, card.Y - gap - o, w + (2 * (gap + o)), o),
            (card.X - gap - o, card.Y + h + gap, w + (2 * (gap + o)), o),
            (card.X - gap - o, card.Y - gap, o, h + (2 * gap)),
            (card.X + w + gap, card.Y - gap, o, h + (2 * gap)),
        }.Select(r => (2 * r.Item1, 2 * r.Item2, 2 * r.Item3, 2 * r.Item4)).ToHashSet();
        var sample = built.Items.OfType<RectFill>().First(r => outline.Contains((r.X, r.Y, r.Width, r.Height)));
        var items = built.Items.Where(i => variant == 'A' || i is not RectFill r || !outline.Contains((r.X, r.Y, r.Width, r.Height))).ToList();
        if (items.Count == built.Items.Count && variant != 'A')
        {
            throw new InvalidOperationException("The card outline was not found on the built page.");
        }

        void Rect(long x, long y, long wide, long high) => items.Add(new RectFill(sample.Layer, sample.Colour, 2 * x, 2 * y, 2 * wide, 2 * high));
        if (variant == 'B')
        {
            long t = HairlineDmm / 2;
            Rect(card.X - t, card.Y - t, w + HairlineDmm, HairlineDmm);
            Rect(card.X - t, card.Y + h - t, w + HairlineDmm, HairlineDmm);
            Rect(card.X - t, card.Y - t, HairlineDmm, h + HairlineDmm);
            Rect(card.X + w - t, card.Y - t, HairlineDmm, h + HairlineDmm);
        }
        else if (variant == 'C')
        {
            long l = card.X - gap - o, r = card.X + w + gap, t = card.Y - gap - o, b = card.Y + h + gap, arm = CornerArmDmm;
            foreach (var (x, y, right, down) in new[] { (l, t, true, true), (r, t, false, true), (l, b, true, false), (r, b, false, false) })
            {
                Rect(right ? x : x + o - arm, y, arm, o);
                Rect(x, down ? y : y + o - arm, o, arm);
            }
        }

        string note = variant switch { 'B' => "edges on the line", 'C' => "inside the corner marks", _ => GridStyle4.CardNote };
        items = [.. items.Select(i => i is TextRun run && run.Text == GridStyle4.CardNote ? run with { Text = note } : i)];
        // In the clear band between the caliper's dashed guide and the card, where no marker, crosshair or word is: a marker covered by the
        // label would cost the page its registration.
        long middle = card.X + (w / 2);
        items.Add(new TextRun(sample.Layer, sample.Colour, 2 * middle, 2 * 930, 2L * GridStyle3.FontSizeForCap(60), $"TEST {variant}, NOT FOR USE", TextAnchor.Centre, true));
        return built with { Items = items };
    }
}
