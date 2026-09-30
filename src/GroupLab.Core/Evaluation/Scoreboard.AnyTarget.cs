using System.Diagnostics;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Evaluation;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 318 section 2: the scoreboard's "any target" class. Targets GroupLab did not print, drawn in code and never
/// kept as pictures: black bulls on white, a fluorescent target whose hits show as bright rings, black diamonds, and a grid, each 6.5 by 8.5 in
/// at 200 dpi with 16 holes of a .308 bullet at known places, read by <see cref="AnyTargetHoleFinder"/> with the scale known, as it is once the
/// person has set it. Each is read clean, under a hard shadow, dim, and curled, the scoreboard's own conditions; a line is "any target, black
/// bulls, dim". docs/scoreboard/any-target-baseline.json holds the lines every build is held to.
/// </summary>
public static partial class Scoreboard
{
    /// <summary>The synthetic targets GroupLab did not print.</summary>
    public static IReadOnlyList<string> AnyTargetKinds { get; } = ["black bulls", "fluorescent", "diamonds", "grid"];

    /// <summary>The conditions each is read under: light (a hard shadow, dim) and geometry (a curl), beside the clean picture.</summary>
    public static IReadOnlyList<string> AnyTargetConditions { get; } = ["clean", "hard shadow", "dim", "curl 15 px"];

    /// <summary>The seeds the committed any-target baseline was measured with.</summary>
    public static IReadOnlyList<int> AnyTargetSeeds { get; } = [318, 319];

    public const double AnyTargetDpi = 200;

    /// <summary>The bullet the holes are made for and the finder is told, inches.</summary>
    public const double AnyTargetCalibre = 0.308;

    public const double AnyTargetWidthInches = 6.5, AnyTargetHeightInches = 8.5;

    /// <summary>Four holes around each of the four aim points.</summary>
    public const int AnyTargetHolesPerAim = 4;

    /// <summary>The synthetic holes' size against the survey's population, so they read about a .308's size.</summary>
    private const double AnyTargetHoleScale = 1.2;

    /// <summary>A line's name: "any target", the kind, the condition.</summary>
    public static string AnyTargetLine(string kind, string condition) => $"any target, {kind}, {condition}";

    /// <summary>The four aim points every kind is drawn around, inches from the top left.</summary>
    private static readonly (double X, double Y)[] AnyTargetAims = [(1.75, 2.25), (4.75, 2.25), (1.75, 6.25), (4.75, 6.25)];

    /// <summary>
    /// A synthetic target GroupLab did not print, as the brightest channel of a photograph square on, with its holes' centres in pixels. The
    /// artwork, where the holes go and each hole's look are drawn from <paramref name="seed"/>. A hole on white paper has a white scanner lid
    /// behind it, as on a scan; on the fluorescent target the board behind is dark, as it is on the range, and each hit has a bright ring.
    /// No hole is placed across the edge of a large dark shape: that case is a known limit, not measured here.
    /// </summary>
    public static (GrayImage Value, IReadOnlyList<PointD> Holes) AnyTargetPicture(string kind, int seed)
    {
        ArgumentNullException.ThrowIfNull(kind);
        double dpi = AnyTargetDpi;
        int w = (int)(AnyTargetWidthInches * dpi), h = (int)(AnyTargetHeightInches * dpi);
        var render = new byte[w * h];
        var random = new Random(seed * 1009 + AnyTargetKinds.ToList().IndexOf(kind));
        bool fluorescent = kind == "fluorescent";
        Array.Fill(render, fluorescent ? (byte)0 : (byte)255);

        // Where a hole may go, from an aim point, and the least distance between two holes, inches.
        Func<double, double, bool> allowed;
        double spread, apart = fluorescent ? 1.05 : 0.4;
        switch (kind)
        {
            case "black bulls":
                foreach (var (x, y) in AnyTargetAims)
                {
                    Disc(render, w, h, dpi, x, y, 1.0, 0);
                    Ring(render, w, h, dpi, x, y, 0.35, 0.025, 255);
                    Ring(render, w, h, dpi, x, y, 0.7, 0.025, 255);
                    Ring(render, w, h, dpi, x, y, 1.3, 0.025, 0);
                    Ring(render, w, h, dpi, x, y, 1.6, 0.025, 0);
                }

                spread = 1.55;
                allowed = (dx, dy) => Math.Abs(Math.Sqrt((dx * dx) + (dy * dy)) - 1.0) > 0.18;
                break;
            case "fluorescent":
                foreach (var (x, y) in AnyTargetAims)
                {
                    Ring(render, w, h, dpi, x, y, 0.5, 0.04, 255);
                    Ring(render, w, h, dpi, x, y, 1.0, 0.04, 255);
                    Box(render, w, h, dpi, x - 1.3, y - 0.02, x + 1.3, y + 0.02, 255);
                    Box(render, w, h, dpi, x - 0.02, y - 1.3, x + 0.02, y + 1.3, 255);
                    Disc(render, w, h, dpi, x, y, 0.25, 255);
                }

                spread = 1.4;
                allowed = (dx, dy) => Math.Sqrt((dx * dx) + (dy * dy)) > 0.55;
                break;
            case "diamonds":
                foreach (var (x, y) in AnyTargetAims)
                {
                    Diamond(render, w, h, dpi, x, y, 1.25, 1.22, 0);
                    Diamond(render, w, h, dpi, x, y, 0.75, 0, 0);
                    Diamond(render, w, h, dpi, x, y, 0.15, 0, 255);
                }

                spread = 1.45;
                allowed = (dx, dy) =>
                {
                    double d = Math.Abs(dx) + Math.Abs(dy);
                    return d > 0.35 && Math.Abs(d - 0.75) > 0.22 && Math.Abs(d - 1.235) > 0.12;
                };
                break;
            case "grid":
                for (double g = 0.25; g <= AnyTargetWidthInches; g += 1)
                {
                    Box(render, w, h, dpi, g - 0.0075, 0, g + 0.0075, AnyTargetHeightInches, 0);
                }

                for (double g = 0.25; g <= AnyTargetHeightInches; g += 1)
                {
                    Box(render, w, h, dpi, 0, g - 0.0075, AnyTargetWidthInches, g + 0.0075, 0);
                }

                foreach (var (x, y) in AnyTargetAims)
                {
                    Disc(render, w, h, dpi, x, y, 0.5, 0);
                }

                spread = 1.5;
                allowed = (dx, dy) => Math.Abs(Math.Sqrt((dx * dx) + (dy * dy)) - 0.5) > 0.18;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "not a synthetic target: " + string.Join(", ", AnyTargetKinds));
        }

        var places = new List<(double X, double Y)>();
        foreach (var (ax, ay) in AnyTargetAims)
        {
            int placed = 0;
            for (int attempt = 0; placed < AnyTargetHolesPerAim && attempt < 5000; attempt++)
            {
                double dx = ((2 * random.NextDouble()) - 1) * spread, dy = ((2 * random.NextDouble()) - 1) * spread;
                double x = ax + dx, y = ay + dy;
                if (Math.Sqrt((dx * dx) + (dy * dy)) > spread || !allowed(dx, dy) || places.Any(p => Math.Sqrt(Math.Pow(p.X - x, 2) + Math.Pow(p.Y - y, 2)) < apart))
                {
                    continue;
                }

                places.Add((x, y));
                placed++;
            }
        }

        if (fluorescent)
        {
            // A hit takes the black top layer off in a ragged patch around the hole and shows the bright layer under it.
            foreach (var (x, y) in places)
            {
                double radius = 0.3 + (0.2 * random.NextDouble());
                double[] lobes = [.. Enumerable.Range(0, 3).Select(_ => 0.15 * random.NextDouble())];
                double[] phases = [.. Enumerable.Range(0, 3).Select(_ => 2 * Math.PI * random.NextDouble())];
                Paint(render, w, h, dpi, x, y, radius * 1.5, (dx, dy) =>
                {
                    double theta = Math.Atan2(dy, dx), edge = radius;
                    for (int k = 0; k < lobes.Length; k++)
                    {
                        edge *= 1 + (lobes[k] * Math.Cos(((k + 2) * theta) + phases[k]));
                    }

                    return Math.Sqrt((dx * dx) + (dy * dy)) <= edge ? 255 : null;
                });
            }
        }

        const double dmm = 254;
        var holes = places.Select(p => SyntheticSheet.SampleHole(random, p.X * dmm, p.Y * dmm,
            render[(Math.Clamp((int)(p.Y * dpi), 0, h - 1) * w) + Math.Clamp((int)(p.X * dpi), 0, w - 1)] < 128,
            fluorescent ? HoleBacking.Dark : HoleBacking.ScannerLid, AnyTargetHoleScale)).ToList();
        double s = dmm / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(new GrayImage(w, h, render), dpi, truth, w, h, holes, [], random);
        return (image, [.. holes.Select(hole => truth.ToImage(new PointD(hole.X, hole.Y)))]);
    }

    /// <summary>
    /// Every synthetic target GroupLab did not print, under every condition of <see cref="AnyTargetConditions"/>, on every seed, read by
    /// <see cref="AnyTargetHoleFinder"/> at the picture's known resolution with the bullet named, and scored hole by hole.
    /// </summary>
    public static IReadOnlyList<PictureScore> RunAnyTarget(IImagingBackend backend, IReadOnlyList<int> seeds, IEnumerable<string>? only = null)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(seeds);
        var scores = new List<PictureScore>();
        foreach (int seed in seeds)
        {
            foreach (string kind in AnyTargetKinds)
            {
                var lines = Conditions.Where(c => AnyTargetConditions.Contains(c.Name) && (only is null || only.Contains(AnyTargetLine(kind, c.Name), StringComparer.OrdinalIgnoreCase))).ToList();
                if (lines.Count == 0)
                {
                    continue;
                }

                var (picture, holes) = AnyTargetPicture(kind, seed);
                foreach (var condition in lines)
                {
                    if (condition.Degrade(picture, new Random(seed * 31), null) is not { } seen)
                    {
                        continue;
                    }

                    var clock = Stopwatch.StartNew();
                    var finding = AnyTargetHoleFinder.Find(seen, AnyTargetDpi, backend, AnyTargetCalibre);
                    clock.Stop();
                    var moved = holes.Select(p => condition.Move(p, seen.Width)).ToList();
                    var marks = finding.Holes.Select(p => p.Image).ToList();
                    var (found, falseMarks, errors) = Match(moved, marks, FoundWithinInches * AnyTargetDpi);
                    var inches = errors.Select(e => e / AnyTargetDpi).ToList();
                    scores.Add(new PictureScore(AnyTargetLine(kind, condition.Name), $"seed {seed}", "holes", holes.Count, marks.Count, found, falseMarks,
                        Median(inches), Worst(inches), null, null, null, true, clock.ElapsedMilliseconds)
                    {
                        CentreErrors = inches,
                    });
                }
            }
        }

        return scores;
    }

    private static void Paint(byte[] render, int w, int h, double dpi, double cx, double cy, double reach, Func<double, double, byte?> shade)
    {
        int x0 = Math.Max(0, (int)((cx - reach) * dpi)), x1 = Math.Min(w - 1, (int)Math.Ceiling((cx + reach) * dpi));
        int y0 = Math.Max(0, (int)((cy - reach) * dpi)), y1 = Math.Min(h - 1, (int)Math.Ceiling((cy + reach) * dpi));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (shade(((x + 0.5) / dpi) - cx, ((y + 0.5) / dpi) - cy) is { } level)
                {
                    render[(y * w) + x] = level;
                }
            }
        }
    }

    private static void Disc(byte[] render, int w, int h, double dpi, double cx, double cy, double radius, byte level) =>
        Paint(render, w, h, dpi, cx, cy, radius, (dx, dy) => (dx * dx) + (dy * dy) <= radius * radius ? level : null);

    private static void Ring(byte[] render, int w, int h, double dpi, double cx, double cy, double radius, double width, byte level) =>
        Paint(render, w, h, dpi, cx, cy, radius + width, (dx, dy) => Math.Abs(Math.Sqrt((dx * dx) + (dy * dy)) - radius) <= width / 2 ? level : null);

    /// <summary>A diamond, a square on its point, of half-diagonal <paramref name="outer"/>, hollow inside <paramref name="inner"/>.</summary>
    private static void Diamond(byte[] render, int w, int h, double dpi, double cx, double cy, double outer, double inner, byte level) =>
        Paint(render, w, h, dpi, cx, cy, outer, (dx, dy) => Math.Abs(dx) + Math.Abs(dy) is var d && d <= outer && d >= inner ? level : null);

    private static void Box(byte[] render, int w, int h, double dpi, double left, double top, double right, double bottom, byte level) =>
        Paint(render, w, h, dpi, (left + right) / 2, (top + bottom) / 2, Math.Max(right - left, bottom - top) / 2, (dx, dy) =>
            Math.Abs(dx) <= (right - left) / 2 && Math.Abs(dy) <= (bottom - top) / 2 ? level : null);
}
