using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Detection;

/// <summary>What shows through a perforation: a scanner's white lid, or a dark board behind a photographed target.</summary>
public enum HoleBacking
{
    ScannerLid,
    Dark,
}

/// <summary>
/// A synthetic bullet hole in page dmm: the darkest rim at <see cref="RimRadius"/>, lobed by
/// <see cref="LobeAmplitudes"/> at harmonics 2 upward, a rim band <see cref="RimWidth"/> wide at <see cref="RimV"/>, a core
/// at <see cref="CoreV"/> where the paper, and any ink on it, is gone, and a disturbed zone outside the rim that recovers
/// to paper over <see cref="ZoneLength"/>.
/// </summary>
public sealed record SyntheticHole(double X, double Y, double RimRadius, double RimWidth, double RimV, double CoreV, double ZoneLength, IReadOnlyList<double> LobeAmplitudes, IReadOnlyList<double> LobePhases);

/// <summary>A straight stroke of hand ink in page dmm, as a capsule of the given width and grey level.</summary>
public sealed record InkStroke(double X0, double Y0, double X1, double Y1, double Width, double V);

/// <summary>
/// Synthetic GroupLab sheets with holes of known centre, docs/PHASE1-BRIEF.md section 4.3, drawn from the distributions
/// docs/SCAN-MEASUREMENTS.md measured on 343 real holes, so render-and-difference can be measured against truth before a
/// shot GroupLab target exists.
/// <list type="bullet">
/// <item>Rim radius from the darkest-rim diameter, 0.2099 plus or minus 0.0518 in (section 3.2).</item>
/// <item>Rim band width and darkness split by paper and ink, 0.0641 plus or minus 0.0389 in at V 33.8 plus or minus 30.0
/// on paper, 0.0824 plus or minus 0.0538 in at V 48.1 plus or minus 43.4 on ink (section 3.6).</item>
/// <item>Core V 192.55 plus or minus 26.70 with a scanner lid behind it (section 3.2), and dark with a board behind it,
/// because a synthetic scan has a white backing and a synthetic photograph does not (brief section 4.3).</item>
/// <item>Paper V near 245.65 with a gentle gradient (section 3.2), black print at V 65 (section 4.1), and the black
/// marker and ballpoint strokes of section 7.1.</item>
/// </list>
/// The rim is lobed, not circular, because the survey measured a coefficient of variation of 0.48 in rim radius; the
/// lobes here are gentler than that, which is a known limit of the synthesis, and <c>grouplab holes synthetic</c> reports
/// how the baseline detector measures these holes against the survey so the realism is a measured quantity.
/// </summary>
public static class SyntheticSheet
{
    /// <summary>Black print on a scan, docs/SCAN-MEASUREMENTS.md section 4.1.</summary>
    public const double InkV = 65;

    private const double DmmPerInch = 254;

    /// <summary>
    /// The darkest rim's radius, mean and sd, inches; the largest amplitude of each of the four rim lobes, as a fraction of
    /// that radius; and the length over which the disturbed zone outside the rim recovers to paper, inches. These are the
    /// synthesis's own constants, not the survey's measurements, and they are set so that the baseline measures the
    /// synthetic holes as the survey measured the real ones (<c>grouplab holes synthetic --realism</c>).
    /// </summary>
    public const double RimRadiusInches = 0.060, RimRadiusSdInches = 0.016, LobeAmplitude = 1.5, ZoneInches = 0.014;

    /// <summary>
    /// The solid rim band's width on paper, mean and sd, inches, and 29 percent wider on ink as section 3.6 measured. The
    /// survey's annulus thickness, 0.0641 in on paper, is a run of pixels below the midpoint between paper and the rim's
    /// darkest, so it includes the zone either side of the band; taken as the band's own width it swallowed the core of a
    /// small hole and made the baseline measure rim as core.
    /// </summary>
    public const double RimWidthInches = 0.035, RimWidthSdInches = 0.020;

    public static SyntheticHole SampleHole(Random random, double x, double y, bool onInk, HoleBacking backing)
    {
        ArgumentNullException.ThrowIfNull(random);
        double Normal(double mean, double sd, double low, double high) => Math.Clamp(mean + (sd * SyntheticSurface.Gaussian(random)), low, high);
        double rimRadius = Normal(RimRadiusInches, RimRadiusSdInches, 0.05, 0.25) * DmmPerInch;
        double rimWidth = (onInk ? Normal(RimWidthInches * 1.29, RimWidthSdInches * 1.29, 0.01, 0.15) : Normal(RimWidthInches, RimWidthSdInches, 0.01, 0.12)) * DmmPerInch;
        double rimV = onInk ? Normal(48.1, 43.4, 2, 169) : Normal(33.8, 30.0, 2, 169);
        double coreV = backing == HoleBacking.ScannerLid ? Normal(192.55, 26.70, 75, 250) : Normal(70, 25, 20, 150);
        double[] amplitudes = [.. Enumerable.Range(0, 4).Select(_ => random.NextDouble() * LobeAmplitude)];
        double[] phases = [.. Enumerable.Range(0, 4).Select(_ => random.NextDouble() * 2 * Math.PI)];
        return new SyntheticHole(x, y, rimRadius, rimWidth, rimV, coreV, ZoneInches * DmmPerInch, amplitudes, phases);
    }

    /// <summary>Two crossing strokes centred on a point, as a shooter marks a hole: the X marks of section 7.1, 0.0625 in wide at grey 98.</summary>
    public static IReadOnlyList<InkStroke> Cross(double x, double y, double length, double width, double v, double angle)
    {
        double h = length / 2;
        return
        [
            new(x - (h * Math.Cos(angle)), y - (h * Math.Sin(angle)), x + (h * Math.Cos(angle)), y + (h * Math.Sin(angle)), width, v),
            new(x - (h * Math.Cos(angle + (Math.PI / 2))), y - (h * Math.Sin(angle + (Math.PI / 2))), x + (h * Math.Cos(angle + (Math.PI / 2))), y + (h * Math.Sin(angle + (Math.PI / 2))), width, v),
        ];
    }

    /// <summary>A closed letter bowl, a ring of strokes: the compact, dark, hole-sized shape five bowls of a caption made (section 7.1).</summary>
    public static IReadOnlyList<InkStroke> Bowl(double x, double y, double outerDiameter, double width, double v)
    {
        double r = (outerDiameter - width) / 2;
        return [.. Enumerable.Range(0, 16).Select(k =>
        {
            double a0 = 2 * Math.PI * k / 16, a1 = 2 * Math.PI * (k + 1) / 16;
            return new InkStroke(x + (r * Math.Cos(a0)), y + (r * Math.Sin(a0)), x + (r * Math.Cos(a1)), y + (r * Math.Sin(a1)), width, v);
        })];
    }

    /// <summary>An arrowhead, two strokes meeting at a point, which closing turns into a hole-sized blob (section 7.1).</summary>
    public static IReadOnlyList<InkStroke> Arrowhead(double x, double y, double length, double width, double v, double angle)
    {
        const double spread = 0.5;
        return
        [
            new(x, y, x - (length * Math.Cos(angle - spread)), y - (length * Math.Sin(angle - spread)), width, v),
            new(x, y, x - (length * Math.Cos(angle + spread)), y - (length * Math.Sin(angle + spread)), width, v),
        ];
    }

    /// <summary>
    /// The observed image: the render resampled through <paramref name="truth"/>, printed at <see cref="InkV"/> on a paper
    /// gradient, hand ink laid over it, holes punched through it, then a Gaussian blur and grey noise.
    /// </summary>
    public static GrayImage Compose(GrayImage render, double dpi, IPageMapping truth, int width, int height, IReadOnlyList<SyntheticHole> holes, IReadOnlyList<InkStroke> strokes,
        Random random, double blurSigmaPixels = 0.6, double noiseSd = 2)
    {
        ArgumentNullException.ThrowIfNull(render);
        ArgumentNullException.ThrowIfNull(truth);
        ArgumentNullException.ThrowIfNull(holes);
        ArgumentNullException.ThrowIfNull(strokes);
        ArgumentNullException.ThrowIfNull(random);
        var expected = ExpectedImage.Render(render, dpi, truth, width, height);
        var v = new float[width * height];
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                double paper = Paper(x, y, width, height);
                v[(y * width) + x] = (float)(InkV + ((paper - InkV) * expected.Pixels[(y * width) + x] / 255.0));
            }
        });

        double pixelsPerDmm = dpi / DmmPerInch;
        foreach (var stroke in strokes)
        {
            var a = truth.ToImage(new PointD(stroke.X0, stroke.Y0));
            var b = truth.ToImage(new PointD(stroke.X1, stroke.Y1));
            double reach = (stroke.Width * pixelsPerDmm) + 2;
            Paint(v, width, height, Math.Min(a.X, b.X) - reach, Math.Min(a.Y, b.Y) - reach, Math.Max(a.X, b.X) + reach, Math.Max(a.Y, b.Y) + reach, truth, (page, current) =>
                Segment(page, stroke) <= stroke.Width / 2 ? Math.Min(current, (float)stroke.V) : current);
        }

        foreach (var hole in holes)
        {
            var centre = truth.ToImage(new PointD(hole.X, hole.Y));
            double reach = ((hole.RimRadius * (1 + hole.LobeAmplitudes.Sum())) + (hole.RimWidth / 2) + (5 * hole.ZoneLength)) * pixelsPerDmm;
            Paint(v, width, height, centre.X - reach, centre.Y - reach, centre.X + reach, centre.Y + reach, truth, (page, current) => Hole(page, hole, current, (int)centre.X, (int)centre.Y, width, height));
        }

        var blurred = blurSigmaPixels > 0 ? Blur(v, width, height, blurSigmaPixels) : v;
        var pixels = new byte[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (byte)Math.Clamp(Math.Round(blurred[i] + (noiseSd * SyntheticSurface.Gaussian(random))), 0, 255);
        }

        return new GrayImage(width, height, pixels);
    }

    /// <summary>
    /// Synthetic holes punched into an image that already exists, through its registration, with no blur or noise added. A scan of a sheet
    /// printed before a change to the artwork, punched, is how the committed corpus has holes on old print: NOTES-FROM-PLANNING.md entry 77
    /// section 3 item 2 asks that a change to printed artwork be checked against sheets already printed, and those sheets have no holes.
    /// </summary>
    public static GrayImage Punch(GrayImage observed, IPageMapping truth, double dpi, IReadOnlyList<SyntheticHole> holes)
    {
        ArgumentNullException.ThrowIfNull(observed);
        ArgumentNullException.ThrowIfNull(truth);
        ArgumentNullException.ThrowIfNull(holes);
        int width = observed.Width, height = observed.Height;
        var v = observed.Pixels.Select(p => (float)p).ToArray();
        double pixelsPerDmm = dpi / DmmPerInch;
        foreach (var hole in holes)
        {
            var centre = truth.ToImage(new PointD(hole.X, hole.Y));
            double reach = ((hole.RimRadius * (1 + hole.LobeAmplitudes.Sum())) + (hole.RimWidth / 2) + (5 * hole.ZoneLength)) * pixelsPerDmm;
            Paint(v, width, height, centre.X - reach, centre.Y - reach, centre.X + reach, centre.Y + reach, truth, (page, current) => Hole(page, hole, current, (int)centre.X, (int)centre.Y, width, height));
        }

        return new GrayImage(width, height, [.. v.Select(p => (byte)Math.Clamp(Math.Round(p), 0, 255))]);
    }

    /// <summary>The paper level the sheet is printed on: 245.65 at the centre, five grey levels across and four down.</summary>
    public static double Paper(double x, double y, int width, int height) => 245.65 + (5 * ((x / width) - 0.5)) - (4 * ((y / height) - 0.5));

    private static void Paint(float[] v, int width, int height, double left, double top, double right, double bottom, IPageMapping truth, Func<PointD, float, float> shade)
    {
        int u0 = Math.Max(0, (int)Math.Floor(left)), u1 = Math.Min(width - 1, (int)Math.Ceiling(right));
        int v0 = Math.Max(0, (int)Math.Floor(top)), v1 = Math.Min(height - 1, (int)Math.Ceiling(bottom));
        for (int y = v0; y <= v1; y++)
        {
            for (int x = u0; x <= u1; x++)
            {
                v[(y * width) + x] = shade(truth.ToPage(new PointD(x, y)), v[(y * width) + x]);
            }
        }
    }

    private static float Hole(PointD page, SyntheticHole hole, float current, int cx, int cy, int width, int height)
    {
        double dx = page.X - hole.X, dy = page.Y - hole.Y, d = Math.Sqrt((dx * dx) + (dy * dy)), theta = Math.Atan2(dy, dx);
        // Lobes reach outward only, as narrow spikes at harmonics 3 to 6, so the rim's inner edge stays at its radius and a
        // lobe cannot eat into the core, where a smooth low harmonic that swings both ways did; narrow spikes give the rim the
        // spread of radius the survey measured without the mean radius a broad lobe adds.
        double lobe = 1;
        for (int k = 0; k < hole.LobeAmplitudes.Count; k++)
        {
            double wave = 0.5 + (0.5 * Math.Cos(((k + 3) * theta) + hole.LobePhases[k]));
            lobe += hole.LobeAmplitudes[k] * Math.Pow(wave, 6);
        }

        double rim = hole.RimRadius * lobe, half = hole.RimWidth / 2;
        if (d < rim - half)
        {
            return (float)hole.CoreV;
        }

        if (d <= rim + half)
        {
            return Math.Min(current, (float)hole.RimV);
        }

        double paper = Paper(cx, cy, width, height);
        double zone = paper - ((paper - hole.RimV) * Math.Exp(-(d - rim - half) / hole.ZoneLength));
        return Math.Min(current, (float)zone);
    }

    private static double Segment(PointD p, InkStroke s)
    {
        double vx = s.X1 - s.X0, vy = s.Y1 - s.Y0, length2 = (vx * vx) + (vy * vy);
        double t = length2 == 0 ? 0 : Math.Clamp((((p.X - s.X0) * vx) + ((p.Y - s.Y0) * vy)) / length2, 0, 1);
        return Math.Sqrt(Math.Pow(p.X - (s.X0 + (t * vx)), 2) + Math.Pow(p.Y - (s.Y0 + (t * vy)), 2));
    }

    private static float[] Blur(float[] source, int width, int height, double sigma)
    {
        int radius = (int)Math.Ceiling(3 * sigma);
        var kernel = new double[(2 * radius) + 1];
        for (int k = -radius; k <= radius; k++)
        {
            kernel[k + radius] = Math.Exp(-(k * k) / (2 * sigma * sigma));
        }

        double sum = kernel.Sum();
        for (int k = 0; k < kernel.Length; k++)
        {
            kernel[k] /= sum;
        }

        var across = new float[source.Length];
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                double acc = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    acc += kernel[k + radius] * source[(y * width) + Math.Clamp(x + k, 0, width - 1)];
                }

                across[(y * width) + x] = (float)acc;
            }
        });

        var result = new float[source.Length];
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                double acc = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    acc += kernel[k + radius] * across[(Math.Clamp(y + k, 0, height - 1) * width) + x];
                }

                result[(y * width) + x] = (float)acc;
            }
        });

        return result;
    }
}
