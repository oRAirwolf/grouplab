using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Detection;

/// <summary>What told a proposed hole apart from the printing around it, on a target GroupLab did not print.</summary>
public enum HoleLook
{
    /// <summary>Darker than the paper around it: the survey's hole, and most holes on most targets.</summary>
    DarkOnPaper,

    /// <summary>Lighter than the print around it, inside a large dark area such as a black bull: what shows through is lighter than the ink.</summary>
    LightInPrint,

    /// <summary>A dark centre inside a bright ring, as a hit on a fluorescent target shows.</summary>
    DarkInBrightRing,
}

/// <summary>
/// A hole proposed on a target GroupLab did not print, NOTES-FROM-PLANNING.md entry 318 section 2: its centre in image pixels, how wide it was
/// measured in inches (for a light hole in print, what shows through it, which is narrower than the bullet), how it looked, and where the
/// finder is unsure of it, why, in words for the person. A proposal with a doubt goes to the review queue.
/// </summary>
public sealed record ProposedHole(PointD Image, double DiameterInches, HoleLook Look, string? Doubt);

/// <summary>Everything one pass of <see cref="AnyTargetHoleFinder"/> found and refused, at the resolution it was given.</summary>
public sealed record AnyTargetFinding(double Dpi, IReadOnlyList<ProposedHole> Holes, IReadOnlyList<RejectedBlob> Rejected);

/// <summary>
/// Holes found automatically on a target GroupLab did not print, NOTES-FROM-PLANNING.md entry 318 section 2 and docs/DETECTION-LEARNING-STUDY.md
/// section 7. There is no printed artwork to subtract, so each hole is told apart from the printing by how it looks against what is around it,
/// three ways, each at the resolution it needs:
/// <list type="bullet">
/// <item>Dark on paper: the neutral darkness detector of docs/SCAN-MEASUREMENTS.md section 3.1, once with the paper's level taken locally (a
/// closing that ignores anything dark narrower than 1.2 in, so a tight group is still on paper) and once with the survey's single level for
/// the picture. The local level keeps holes in a shadow and in dim light, where darkness is taken as a share of the paper; the single level
/// keeps a hole beside a large printed shape. A local mark is refused when much of the ring around it lies on large dark print, which is what
/// the corner of a large printed shape is.</item>
/// <item>Light in print: inside a black bull what shows through a hole is lighter than the ink. The print's level is taken locally (an opening
/// that ignores anything light narrower than 0.6 in), printed light lines narrower than 0.05 in are erased, and a round light spot with dark
/// print all round it is a hole.</item>
/// <item>A dark centre inside a bright ring: a hit on a fluorescent target, looked for only where most of the picture is dark print. Each small,
/// round dark piece with bright all round it is a hit's centre; a bright patch without one is printing, and is refused rather than proposed.
/// Where hits were found, light spots are not looked for, because a piece of a ring is one.</item>
/// </list>
/// The three are merged, one proposal for one hole. The finder proposes and never decides: every proposal is shown to the person as a mark to
/// confirm, move or remove, and one it is unsure of carries its reason to the review queue.
/// </summary>
public static class AnyTargetHoleFinder
{
    /// <summary>Dark holes are found at no more than this resolution, dots an inch: the survey's detector reads holes well at 300.</summary>
    public const double HoleDpi = 300;

    /// <summary>The local paper and print levels, and light holes, at about this resolution, dots an inch.</summary>
    public const double PrintDpi = 100;

    /// <summary>Light holes in print at no more than this resolution, dots an inch: what shows through a hole in ink can be small.</summary>
    public const double LightDpi = 200;

    /// <summary>The closing that gives the local paper level ignores anything dark narrower than twice this, inches.</summary>
    public const double PaperRadiusInches = 0.6;

    /// <summary>The opening that gives the local print level ignores anything light narrower than twice this, inches.</summary>
    public const double PrintRadiusInches = 0.3;

    /// <summary>Printed light lines narrower than twice this are erased before light holes are looked for, inches.</summary>
    public const double LineRadiusInches = 0.025;

    /// <summary>How much lighter than the print around it a light hole must be, grey levels.</summary>
    public const double LightStep = 40;

    /// <summary>How much brighter than the print around it a ring must be, grey levels.</summary>
    public const double RingStep = 50;

    /// <summary>The widest light hole in print, inches; anything wider is a printed light area.</summary>
    public const double LargestLightInches = 0.6;

    /// <summary>A bright ring's size, inches across, from the smallest to the largest a hit on a fluorescent target leaves.</summary>
    public const double SmallestRingInches = 0.25, LargestRingInches = 1.4;

    /// <summary>A proposal nearer the picture's edge than this, inches, is one to check.</summary>
    public const double EdgeInches = 0.3;

    /// <summary>The share of the picture that must be dark print before bright rings are looked for: a fluorescent target is mostly black.</summary>
    public const double DarkTarget = 0.5;

    /// <summary>A mark with more than this share of the ring around it dark print is part of a larger printed shape.</summary>
    public const double JoinedToPrint = 0.35;

    /// <param name="value">max(R, G, B) per pixel, as the survey's detector reads it.</param>
    /// <param name="dpi">Pixels an inch on the target, from the scale the person set.</param>
    /// <param name="calibreInches">The bullet's diameter, where the person named one: it sets the smallest hole and what counts as too large.</param>
    /// <param name="inside">Where holes may be, in image pixels, such as inside the rectangle the person set the scale with; null for anywhere.</param>
    public static AnyTargetFinding Find(GrayImage value, double dpi, IImagingBackend backend, double? calibreInches = null, Func<PointD, bool>? inside = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(backend);
        if (dpi <= 0 || double.IsNaN(dpi))
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "the resolution must be positive");
        }

        double? calibre = calibreInches is > 0 ? calibreInches : null;
        var rejected = new List<RejectedBlob>();

        // The local levels, at about 100 dots an inch: the paper's, ignoring dark things narrower than 1.2 in, and the print's, ignoring light
        // things narrower than 0.6 in.
        int fp = Math.Max(1, (int)Math.Floor(dpi / PrintDpi));
        var print = Reduce(value, fp);
        double pdpi = dpi / fp;
        var paperLevel = backend.Morphology(print, MorphologyOperation.Close, Radius(PaperRadiusInches * pdpi));
        // The print's level closed again, because a hole's own dark rim, darker than the ink, pulls the opening down around it by as much as a
        // light hole stands above the ink.
        var printLevel = backend.Morphology(backend.Morphology(print, MorphologyOperation.Open, Radius(PrintRadiusInches * pdpi)), MorphologyOperation.Close, Radius(PrintRadiusInches * pdpi));

        // Bright rings are looked for only on a target that is mostly dark print, as a fluorescent one is: on paper, a printed bullseye's black
        // center in its white ring looks the same, and is not a hit.
        double brightest = Percentile(print.Pixels, 0.95);
        bool mostlyPrint = print.Pixels.Count(p => p < 0.5 * brightest) >= DarkTarget * print.Pixels.Length;
        var patches = new List<(PointD Centre, double Radius)>();
        var rings = mostlyPrint ? Rings(print, fp, dpi, printLevel, paperLevel, backend, calibre, rejected, patches) : [];
        var dark = Dark(value, dpi, fp, paperLevel, backend, calibre, rejected);

        // Where hits with bright rings were found, a light spot is a piece of a ring, not a hole in print; elsewhere, one inside a bright patch is.
        var light = rings.Count > 0 ? [] : Light(value, dpi, fp, printLevel, backend, calibre, rejected).Where(l => !patches.Any(p => Distance(p.Centre, l.Image) < p.Radius)).ToList();

        // One proposal for one hole: a ring's centre first, as the most particular, then the dark holes, then the light ones.
        double apart = 0.5 * Math.Max(calibre ?? 0.2, 0.2) * dpi;
        var holes = new List<ProposedHole>();
        foreach (var hole in rings.Concat(dark).Concat(light))
        {
            if (inside is not null && !inside(hole.Image))
            {
                rejected.Add(new RejectedBlob(hole.Image.X, hole.Image.Y, hole.DiameterInches, "outside the target"));
                continue;
            }

            if (holes.Any(h => Distance(h.Image, hole.Image) < apart))
            {
                continue;
            }

            // A scan's edge and a photograph's are where shadows and the scanner's own border lie.
            double edge = Math.Min(Math.Min(hole.Image.X, value.Width - 1 - hole.Image.X), Math.Min(hole.Image.Y, value.Height - 1 - hole.Image.Y));
            holes.Add(edge < EdgeInches * dpi && hole.Doubt is null ? hole with { Doubt = "it is at the edge of the picture" } : hole);
        }

        return new AnyTargetFinding(dpi, [.. holes.OrderBy(h => h.Image.Y).ThenBy(h => h.Image.X)], rejected);
    }

    /// <summary>
    /// Holes darker than the paper around them, by the survey's detector against the local paper level, then against the survey's single level
    /// for the picture, in the full picture's pixels. The local level keeps holes in a shadow; the single level keeps a hole beside a large dark
    /// printed shape, where the local level is pulled down, and is what the survey measured. The two are merged by the caller.
    /// </summary>
    private static List<ProposedHole> Dark(GrayImage value, double dpi, int fp, GrayImage paperLevel, IImagingBackend backend, double? calibre, List<RejectedBlob> rejected)
    {
        int fa = Math.Max(1, (int)Math.Ceiling((dpi / HoleDpi) - 1e-9));
        var image = Reduce(value, fa);
        double adpi = dpi / fa;
        var paperImage = DrawnUp(paperLevel, fp, image, fa);
        var options = new HoleDetectionOptions(CalibreInches: calibre);
        var holes = new List<ProposedHole>();
        foreach (bool local in new[] { true, false })
        {
            var found = NeutralDarknessHoleDetector.Detect(image, adpi, backend, options, local ? paperImage : null);
            if (local)
            {
                rejected.AddRange(found.Rejected.Select(r => r with { X = Up(r.X, fa), Y = Up(r.Y, fa) }));
            }

            holes.AddRange(Judged(found, fa, paperImage, calibre, rejected, local));
        }

        return holes;
    }

    /// <summary>The dark pass's marks judged: refused where they are the edge of a large printed area, and doubted where they are unlike a hole.</summary>
    private static IEnumerable<ProposedHole> Judged(HoleDetection found, int fa, GrayImage paperImage, double? calibre, List<RejectedBlob> rejected, bool local)
    {
        foreach (var hole in found.Holes)
        {
            // The ring around the mark, from 1.6 to 2.4 of its radius, on the paper level: where that is dark, the ring lies on a printed area too
            // large for the closing to reach into, and the mark is its corner or its edge, not a hole. The other holes of a tight group are not
            // such an area, because the closing reaches over them. Dark is well below the paper at the mark, so a shadow's edge is not print.
            double own = Sample(paperImage, hole.X, hole.Y);
            double dark = 0.75 * own;
            double share = RingShare(paperImage, hole.X, hole.Y, hole.DiameterPixels / 2, 1.6, 2.4, v => v < dark);
            var at = new PointD(Up(hole.X, fa), Up(hole.Y, fa));

            // Against the single level a large printed shape is one mark too large to be a hole, so a mark beside it is only doubted.
            if (local && share > JoinedToPrint)
            {
                rejected.Add(new RejectedBlob(at.X, at.Y, hole.DiameterInches, Words($"joined to printing, {100 * share:0} percent of the ring around it dark")));
                continue;
            }

            string? doubt = SizeDoubt(hole.DiameterInches, calibre, 0.6, 1.7)
                ?? (hole.Solidity < 0.7 ? "its outline is ragged, as printing or a tear can be" : null)
                ?? (share > 0.2 ? "it touches printing" : null);
            yield return new ProposedHole(at, hole.DiameterInches, HoleLook.DarkOnPaper, doubt);
        }
    }

    /// <summary>Holes lighter than the print around them, at up to 200 dots an inch, in the full picture's pixels.</summary>
    private static List<ProposedHole> Light(GrayImage value, double dpi, int fp, GrayImage printLow, IImagingBackend backend, double? calibre, List<RejectedBlob> rejected)
    {
        int fl = Math.Max(1, (int)Math.Floor(dpi / LightDpi));
        var print = Reduce(value, fl);
        double pdpi = dpi / fl;
        var printLevel = DrawnUp(printLow, fp, print, fl);
        int w = print.Width, h = print.Height;
        var residue = new byte[print.Pixels.Length];
        for (int i = 0; i < residue.Length; i++)
        {
            residue[i] = (byte)Math.Clamp(print.Pixels[i] - printLevel.Pixels[i], 0, 255);
        }

        var cleaned = backend.Morphology(new GrayImage(w, h, residue), MorphologyOperation.Open, Radius(LineRadiusInches * pdpi, 1));
        var binary = new byte[residue.Length];
        for (int i = 0; i < binary.Length; i++)
        {
            binary[i] = cleaned.Pixels[i] >= LightStep ? (byte)1 : (byte)0;
        }

        var mask = new GrayImage(w, h, binary);

        // What shows through a hole in ink is never much wider than the bullet; a wider light spot is printed.
        double smallest = Math.Max(HoleSizeGate.AbsoluteFloorInches, 0.25 * (calibre ?? 0.24));
        double largest = calibre is { } bullet ? Math.Min(LargestLightInches, 1.5 * bullet) : LargestLightInches;
        var holes = new List<ProposedHole>();

        // Not filled, as the dark pass's blobs are: a narrow band of paper between printed rings reads as light too, and filling its outline
        // would swallow every hole inside it.
        foreach (var blob in Blobs(binary, w, h, (int)Math.Ceiling(LargestRingInches * pdpi)))
        {
            var (area, cx, cy) = Polygon(blob.Hull);
            if (area <= 0)
            {
                continue;
            }

            double diameter = 2 * Math.Sqrt(area / Math.PI) / pdpi;
            double solidity = blob.Area / area;
            double aspect = Math.Max(blob.Width, blob.Height) / Math.Max(1.0, Math.Min(blob.Width, blob.Height));
            double round = 2 * Math.Sqrt(area / Math.PI) / Math.Max(1.0, Math.Max(blob.Width, blob.Height));
            double radius = Math.Sqrt(area / Math.PI);
            var at = new PointD(Up(cx, fl), Up(cy, fl));
            if (diameter < smallest || diameter > largest)
            {
                // Only a light patch of about a hole's size is worth naming as refused; the rest is paper and print.
                if (diameter >= smallest && diameter <= LargestRingInches)
                {
                    rejected.Add(new RejectedBlob(at.X, at.Y, diameter, Words($"a light patch in print {diameter:0.00} in across, wider than a hole")));
                }

                continue;
            }

            string? why = solidity < 0.6 ? Words($"a light shape in print, not compact, solidity {solidity:0.00}")
                : aspect > 2.0 ? Words($"a light shape in print, elongated, aspect {aspect:0.00}")
                : round < 0.65 || round > 1.08 ? Words($"a light shape in print, not round, {round:0.00}")
                : round < 0.9 && solidity > 0.95 ? Words($"a light shape in print with straight sides, {round:0.00} round")
                : null;

            // A ring, dark at its centre, is a hit on a fluorescent target and is the ring pass's; and a light spot needs print all round it.
            why ??= RingShare(mask, cx, cy, radius, 0, 0.45, v => v > 0) < 0.6 ? "a light ring, not a light spot" : null;
            double core = Sample(print, cx, cy);
            double level = Sample(printLevel, cx, cy);
            why ??= RingShare(print, cx, cy, radius, 1.6, 2.4, v => v < level + (0.5 * (core - level))) < 0.7 ? "a light spot without print all round it" : null;
            if (why is not null)
            {
                rejected.Add(new RejectedBlob(at.X, at.Y, diameter, why));
                continue;
            }

            string? doubt = SizeDoubt(diameter, calibre, 0.25, 1.2) ?? (core - level < 0.35 * core ? "it is only a little lighter than the print around it" : null);
            holes.Add(new ProposedHole(at, diameter, HoleLook.LightInPrint, doubt));
        }

        return holes;
    }

    /// <summary>
    /// Hits on a fluorescent target, at the print level's resolution: the bright, with printed bright lines narrower than 0.07 in taken out and
    /// then dark spikes narrower than 0.1 in (the torn paper around a hole), and each small dark piece the bright then surrounds is a hit's
    /// centre, in the full picture's pixels. Every bright patch of a hit's size is kept in <paramref name="patches"/> (centre and radius, full
    /// picture's pixels) so a piece of one is not proposed again as a light hole.
    /// </summary>
    private static List<ProposedHole> Rings(GrayImage print, int fp, double dpi, GrayImage printLevel, GrayImage brightLevel, IImagingBackend backend, double? calibre,
        List<RejectedBlob> rejected, List<(PointD Centre, double Radius)> patches)
    {
        double pdpi = dpi / fp;
        int w = print.Width, h = print.Height;

        // Bright is well above the print, and above halfway to the bright around it, so a hole's grey centre inside its ring is not.
        var lit = new byte[print.Pixels.Length];
        for (int i = 0; i < lit.Length; i++)
        {
            int v = print.Pixels[i], level = printLevel.Pixels[i];
            lit[i] = v - level >= RingStep && v >= (level + brightLevel.Pixels[i]) / 2.0 ? (byte)255 : (byte)0;
        }

        var cleaned = backend.Morphology(backend.Morphology(new GrayImage(w, h, lit), MorphologyOperation.Open, Radius(0.035 * pdpi, 1)), MorphologyOperation.Close, Radius(0.05 * pdpi, 1));
        var bright = new byte[lit.Length];
        var dark = new byte[lit.Length];
        for (int i = 0; i < lit.Length; i++)
        {
            bright[i] = cleaned.Pixels[i] > 0 ? (byte)1 : (byte)0;
            dark[i] = (byte)(1 - bright[i]);
        }

        foreach (var patch in Blobs(bright, w, h, (int)Math.Ceiling(LargestRingInches * pdpi)))
        {
            var (area, cx, cy) = Polygon(patch.Hull);
            double across = 2 * Math.Sqrt(Math.Max(area, patch.Area) / Math.PI) / pdpi;
            if (across >= SmallestRingInches && !double.IsNaN(cx))
            {
                patches.Add((new PointD(Up(cx, fp), Up(cy, fp)), across * dpi / 2));
            }
        }

        double smallestCore = Math.Max(HoleSizeGate.AbsoluteFloorInches, 0.3 * (calibre ?? 0.24));
        double largestCore = 1.7 * (calibre ?? 0.35);
        var holes = new List<ProposedHole>();
        foreach (var piece in Blobs(dark, w, h, (int)Math.Ceiling(2 * largestCore * pdpi)))
        {
            if (piece.Left == 0 || piece.Top == 0 || piece.Left + piece.Width == w || piece.Top + piece.Height == h)
            {
                continue;
            }

            // Measured by its outline, so a hole whose light centre shows inside its dark rim counts at its whole size.
            var (hullArea, px, py) = Polygon(piece.Hull);
            double core = 2 * Math.Sqrt(Math.Max(hullArea, piece.Area) / Math.PI) / pdpi;
            double aspect = Math.Max(piece.Width, piece.Height) / Math.Max(1.0, Math.Min(piece.Width, piece.Height));
            if (core < smallestCore || core > largestCore || double.IsNaN(px))
            {
                continue;
            }

            // A dark centre is round and has the bright all round it; a dark gap between printed bright shapes is neither.
            var at = new PointD(Up(px, fp), Up(py, fp));
            double around = RingShare(cleaned, px, py, core * pdpi / 2, 1.4, 2.2, v => v > 0);
            if (aspect > 2.5 || around < 0.6)
            {
                rejected.Add(new RejectedBlob(at.X, at.Y, core, Words($"a dark piece in bright print, not a hit's center: {100 * around:0} percent bright around it, aspect {aspect:0.00}")));
                continue;
            }

            holes.Add(new ProposedHole(at, core, HoleLook.DarkInBrightRing, SizeDoubt(core, calibre, 0.3, 1.4)));
        }

        foreach (var (centre, radius) in patches.Where(p => !holes.Any(hole => Distance(hole.Image, p.Centre) < p.Radius)))
        {
            rejected.Add(new RejectedBlob(centre.X, centre.Y, 2 * radius / dpi, Words($"a bright patch {2 * radius / dpi:0.00} in across with no dark center, printing rather than a hit")));
        }

        return holes;
    }

    /// <summary>A sentence when a hole's size is out of keeping with the bullet, from <paramref name="low"/> to <paramref name="high"/> of it.</summary>
    private static string? SizeDoubt(double inches, double? calibre, double low, double high)
    {
        if (calibre is { } bullet)
        {
            return inches > high * bullet ? Words($"it is {inches:0.00} in across, wider than one bullet: it may be two holes, or printing")
                : inches < low * bullet ? Words($"it is {inches:0.00} in across, small for the bullet: it may be printing or a speck")
                : null;
        }

        return inches > 0.5 ? Words($"it is {inches:0.00} in across: it may be two holes, or printing") : null;
    }

    /// <summary>The share of samples on rings from <paramref name="from"/> to <paramref name="to"/> times <paramref name="radius"/> that pass the test.</summary>
    private static double RingShare(GrayImage image, double cx, double cy, double radius, double from, double to, Func<double, bool> test)
    {
        int passed = 0, total = 0;
        for (double k = from; k <= to + 1e-9; k += Math.Max(0.1, (to - from) / 4))
        {
            double r = Math.Max(k * radius, k == 0 ? 0 : 1);
            int angles = r == 0 ? 1 : 48;
            for (int a = 0; a < angles; a++)
            {
                double angle = 2 * Math.PI * a / angles;
                double x = cx + (r * Math.Cos(angle)), y = cy + (r * Math.Sin(angle));
                if (x < 0 || y < 0 || x > image.Width - 1 || y > image.Height - 1)
                {
                    continue;
                }

                total++;
                if (test(Sample(image, x, y)))
                {
                    passed++;
                }
            }
        }

        return total == 0 ? 0 : (double)passed / total;
    }

    private static string Words(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static int Radius(double pixels, int least = 2) => Math.Max(least, (int)Math.Round(pixels, MidpointRounding.ToEven));

    /// <summary>Where a pixel of a picture reduced by <paramref name="k"/> is in the picture: the middle of the k by k block it averages.</summary>
    private static double Up(double reduced, int k) => (reduced * k) + ((k - 1) / 2.0);

    /// <summary>
    /// A level measured on the picture reduced by <paramref name="from"/>, drawn at every pixel of <paramref name="onto"/>, the picture reduced by
    /// <paramref name="to"/>: a pixel there is at (x to + (to - 1) / 2) in the picture, and so at that less (from - 1) / 2, over from, here.
    /// </summary>
    private static GrayImage DrawnUp(GrayImage level, int from, GrayImage onto, int to)
    {
        var pixels = new byte[onto.Pixels.Length];
        Parallel.For(0, onto.Height, y =>
        {
            double ly = (Up(y, to) - ((from - 1) / 2.0)) / from;
            for (int x = 0; x < onto.Width; x++)
            {
                pixels[(y * onto.Width) + x] = (byte)Math.Round(Sample(level, (Up(x, to) - ((from - 1) / 2.0)) / from, ly));
            }
        });
        return new GrayImage(onto.Width, onto.Height, pixels);
    }

    /// <summary>A box average by <paramref name="k"/> in each direction.</summary>
    internal static GrayImage Reduce(GrayImage image, int k)
    {
        if (k <= 1)
        {
            return image;
        }

        int w = image.Width / k, h = image.Height / k;
        var pixels = new byte[w * h];
        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                int sum = 0;
                for (int dy = 0; dy < k; dy++)
                {
                    int row = ((y * k) + dy) * image.Width;
                    for (int dx = 0; dx < k; dx++)
                    {
                        sum += image.Pixels[row + (x * k) + dx];
                    }
                }

                pixels[(y * w) + x] = (byte)(sum / (k * k));
            }
        });
        return new GrayImage(w, h, pixels);
    }

    /// <summary>Bilinear sampling with the border replicated.</summary>
    private static double Sample(GrayImage image, double x, double y)
    {
        x = Math.Clamp(x, 0, image.Width - 1);
        y = Math.Clamp(y, 0, image.Height - 1);
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y), x1 = Math.Min(x0 + 1, image.Width - 1), y1 = Math.Min(y0 + 1, image.Height - 1);
        double fx = x - x0, fy = y - y0;
        var p = image.Pixels;
        int w = image.Width;
        return ((1 - fy) * (((1 - fx) * p[(y0 * w) + x0]) + (fx * p[(y0 * w) + x1]))) + (fy * (((1 - fx) * p[(y1 * w) + x0]) + (fx * p[(y1 * w) + x1])));
    }

    /// <summary>
    /// The 8-connected blobs of a binary image as they are, not filled, each with its pixel count, bounding box and the convex hull of its
    /// pixels' centres; one wider or taller than <paramref name="largest"/> pixels is passed over whole.
    /// </summary>
    private static IEnumerable<ImageBlob> Blobs(byte[] binary, int w, int h, int largest)
    {
        var seen = new bool[binary.Length];
        var stack = new Stack<int>();
        var rows = new Dictionary<int, (int Min, int Max)>();
        for (int start = 0; start < binary.Length; start++)
        {
            if (binary[start] == 0 || seen[start])
            {
                continue;
            }

            int count = 0, left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
            rows.Clear();
            seen[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                int x = i % w, y = i / w;
                count++;
                (left, right, top, bottom) = (Math.Min(left, x), Math.Max(right, x), Math.Min(top, y), Math.Max(bottom, y));
                rows[y] = rows.TryGetValue(y, out var row) ? (Math.Min(row.Min, x), Math.Max(row.Max, x)) : (x, x);
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                        {
                            continue;
                        }

                        int n = (ny * w) + nx;
                        if (binary[n] != 0 && !seen[n])
                        {
                            seen[n] = true;
                            stack.Push(n);
                        }
                    }
                }
            }

            if (right - left + 1 > largest || bottom - top + 1 > largest)
            {
                continue;
            }

            // Only the ends of each row can be on the hull.
            var ends = rows.OrderBy(r => r.Key).SelectMany(r => new[] { new PointD(r.Value.Min, r.Key), new PointD(r.Value.Max, r.Key) }).ToList();
            yield return new ImageBlob(count, left, top, right - left + 1, bottom - top + 1, Hull(ends));
        }
    }

    /// <summary>The convex hull of points, counter-clockwise, by Andrew's monotone chain.</summary>
    private static List<PointD> Hull(List<PointD> points)
    {
        var sorted = points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        if (sorted.Count < 3)
        {
            return sorted;
        }

        static double Cross(PointD o, PointD a, PointD b) => ((a.X - o.X) * (b.Y - o.Y)) - ((a.Y - o.Y) * (b.X - o.X));
        var hull = new List<PointD>();
        foreach (var pass in new[] { sorted, Enumerable.Reverse(sorted).ToList() })
        {
            int floor = hull.Count;
            foreach (var p in pass)
            {
                while (hull.Count >= floor + 2 && Cross(hull[^2], hull[^1], p) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(p);
            }

            hull.RemoveAt(hull.Count - 1);
        }

        return hull;
    }

    /// <summary>A polygon's area and centroid, as OpenCV's contour moments give them.</summary>
    private static (double Area, double X, double Y) Polygon(IReadOnlyList<PointD> hull)
    {
        double a = 0, sx = 0, sy = 0;
        for (int i = 0; i < hull.Count; i++)
        {
            var p = hull[i];
            var q = hull[(i + 1) % hull.Count];
            double cross = (p.X * q.Y) - (q.X * p.Y);
            a += cross;
            sx += (p.X + q.X) * cross;
            sy += (p.Y + q.Y) * cross;
        }

        return a == 0 ? (0, double.NaN, double.NaN) : (Math.Abs(a) / 2, sx / (3 * a), sy / (3 * a));
    }

    /// <summary>The level below which <paramref name="q"/> of the pixels lie.</summary>
    private static int Percentile(byte[] pixels, double q)
    {
        var counts = new long[256];
        foreach (byte p in pixels)
        {
            counts[p]++;
        }

        long wanted = (long)(q * pixels.Length), seen = 0;
        for (int level = 0; level < 256; level++)
        {
            seen += counts[level];
            if (seen > wanted)
            {
                return level;
            }
        }

        return 255;
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
}
