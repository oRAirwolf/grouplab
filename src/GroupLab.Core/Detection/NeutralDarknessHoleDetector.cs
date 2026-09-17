using GroupLab.Core.Imaging;

namespace GroupLab.Core.Detection;

/// <summary>
/// The settings of <see cref="NeutralDarknessHoleDetector"/>, in inches and grey levels, defaulting to the values
/// <c>tools/scan_analysis/s05_holes.py</c> measured the 343 holes of docs/SCAN-MEASUREMENTS.md section 3 with.
/// </summary>
public sealed record HoleDetectionOptions(
    double OpenRadiusInches = 0.032,
    double CloseRadiusInches = 0.055,
    double DarknessThreshold = 28,
    double MinimumDiameterInches = 0.15,
    double MaximumDiameterInches = 0.60,
    double MinimumSolidity = 0.55,
    double MaximumAspect = 2.2);

/// <summary>
/// A detected hole, image pixels: the convex hull's centroid and equivalent diameter, the blob's pixel count over the
/// hull's area, and the survey's appearance measures from the V channel, which is max(R, G, B).
/// </summary>
public sealed record DetectedHole(double X, double Y, double DiameterPixels, double DiameterInches, double Solidity, double PaperV, double CoreMeanV, double AnnulusMinimumV, double RaggednessInches = double.NaN);

/// <summary>
/// A blob refused by a size, compactness or elongation filter, with the reason. <see cref="Zone"/> names the exclusion zone when that is
/// what refused it: the blob passed every filter a hole has to pass and was refused only for where it lies (NOTES-FROM-PLANNING.md entry 77
/// section 3).
/// </summary>
public sealed record RejectedBlob(double X, double Y, double DiameterInches, string Reason, string? Zone = null);

/// <summary>Everything one detection pass found, with the resolution and paper level it used.</summary>
public sealed record HoleDetection(double Dpi, double PaperLevel, IReadOnlyList<DetectedHole> Holes, IReadOnlyList<RejectedBlob> Rejected);

/// <summary>
/// The hole-detection primitive of docs/SCAN-MEASUREMENTS.md section 3.1, ported from <c>tools/scan_analysis/s05_holes.py</c>
/// as the baseline of docs/PHASE1-BRIEF.md section 4.1, which render-and-difference has to beat.
/// <para>
/// <c>Dn = paper - max(R, G, B)</c>, opened with a disk of radius 0.032 in, thresholded, closed with a disk of radius 0.055
/// in, filled, and each blob measured by its convex hull. Three facts from the survey make it work, and they are why the
/// constants are what they are.
/// </para>
/// <list type="bullet">
/// <item>The opening disk, 0.064 in across, is wider than the widest printed stroke measured anywhere in the corpus, a
/// 0.0567 in barcode bar against a 0.0525 in ring stroke, so opening erases printed artwork and cannot erase a hole.</item>
/// <item>Neutral darkness is near zero on red, pink and blue artwork, because those keep one channel at paper level; a hole
/// is achromatic. Black and grey artwork is not separated this way, which is what the opening is for.</item>
/// <item>The rim, not the core, is the signal: a scanner's white lid shows through an open perforation at paper level, so
/// the closing bridges a ragged rim and the convex hull recovers a hole whose rim is a C rather than an O.</item>
/// </list>
/// <para>
/// Pixel-level morphology and blob extraction are the imaging backend's, the operations the survey called in OpenCV; the
/// arithmetic, the filters and the measurements are here, in the survey's order, so the port can be checked against it.
/// </para>
/// </summary>
public static class NeutralDarknessHoleDetector
{
    /// <param name="maxChannel">max(R, G, B) per pixel, which is also HSV Value.</param>
    /// <param name="dpi">The image's resolution; every inch figure in <paramref name="options"/> is scaled by it.</param>
    public static HoleDetection Detect(GrayImage maxChannel, double dpi, IImagingBackend backend, HoleDetectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(maxChannel);
        ArgumentNullException.ThrowIfNull(backend);
        options ??= new HoleDetectionOptions();
        double paper = Percentile(maxChannel.Pixels, 0.90);

        // Neutral darkness, clipped at 0 and truncated to a byte as numpy's astype does.
        var dn = new byte[maxChannel.Pixels.Length];
        for (int i = 0; i < dn.Length; i++)
        {
            dn[i] = (byte)Math.Clamp(paper - maxChannel.Pixels[i], 0, 255);
        }

        int openRadius = Math.Max(2, (int)Math.Round(options.OpenRadiusInches * dpi, MidpointRounding.ToEven));
        var opened = backend.Morphology(new GrayImage(maxChannel.Width, maxChannel.Height, dn), MorphologyOperation.Open, openRadius);
        var binary = new byte[opened.Pixels.Length];
        for (int i = 0; i < binary.Length; i++)
        {
            binary[i] = opened.Pixels[i] >= options.DarknessThreshold ? (byte)1 : (byte)0;
        }

        int closeRadius = Math.Max(3, (int)Math.Round(options.CloseRadiusInches * dpi, MidpointRounding.ToEven));
        var closed = backend.Morphology(new GrayImage(maxChannel.Width, maxChannel.Height, binary), MorphologyOperation.Close, closeRadius);
        double smallest = Math.PI * Math.Pow(options.MinimumDiameterInches * dpi / 2, 2);
        double largest = Math.PI * Math.Pow(options.MaximumDiameterInches * dpi / 2, 2);

        var holes = new List<DetectedHole>();
        var rejected = new List<RejectedBlob>();
        foreach (var blob in backend.FilledBlobs(closed))
        {
            var (area, cx, cy) = Polygon(blob.Hull);
            if (area <= 0)
            {
                continue;
            }

            double diameter = 2 * Math.Sqrt(area / Math.PI);
            double solidity = blob.Area / area;
            double aspect = Math.Max(blob.Width, blob.Height) / Math.Max(1.0, Math.Min(blob.Width, blob.Height));
            string? why = area < smallest ? FormattableString.Invariant($"too small, {diameter / dpi:0.000} in")
                : area > largest ? FormattableString.Invariant($"too large, {diameter / dpi:0.000} in")
                : solidity < options.MinimumSolidity ? FormattableString.Invariant($"not compact, hull solidity {solidity:0.00}")
                : aspect > options.MaximumAspect ? FormattableString.Invariant($"elongated, aspect {aspect:0.00}")
                : null;
            if (why is not null)
            {
                rejected.Add(new RejectedBlob(cx, cy, diameter / dpi, why));
                continue;
            }

            var (paperV, coreMean, annulusMinimum, ragged) = Characterise(maxChannel, cx, cy, diameter / 2, dpi);
            holes.Add(new DetectedHole(cx, cy, diameter, diameter / dpi, solidity, paperV, coreMean, annulusMinimum, ragged / dpi));
        }

        return new HoleDetection(dpi, paper, holes, rejected);
    }

    /// <summary>
    /// The survey's per-hole measures: V sampled on 360 rays at half-pixel steps out to 2.6 hull radii; paper is the median of
    /// the angle-averaged profile's outer 12 percent; each ray's darkest sample beyond 0.3 radii gives the annulus, its 5th
    /// percentile the annulus minimum and its median radius the annulus radius; the core is everything inside 0.45 of that.
    /// Raggedness is the standard deviation of the darkest radius over angle, pixels.
    /// </summary>
    private static (double Paper, double CoreMean, double AnnulusMinimum, double Raggedness) Characterise(GrayImage v, double cx, double cy, double radius, double dpi)
    {
        const double step = 0.5;
        const int angles = 360;
        double reach = Math.Max(radius * 2.6, radius + (0.08 * dpi));
        int samples = (int)Math.Ceiling(reach / step);
        var profile = new double[angles, samples];
        for (int a = 0; a < angles; a++)
        {
            double angle = 2 * Math.PI * a / angles, cos = Math.Cos(angle), sin = Math.Sin(angle);
            for (int k = 0; k < samples; k++)
            {
                profile[a, k] = Replicate(v, cx + (k * step * cos), cy + (k * step * sin));
            }
        }

        var mean = Enumerable.Range(0, samples).Select(k => Enumerable.Range(0, angles).Average(a => profile[a, k])).ToArray();
        double paper = Median(mean.Skip((int)(samples * 0.88)).ToList());
        int inner = Math.Max(1, (int)(0.30 * radius / step));
        var rayMinimum = new double[angles];
        var rayRadius = new double[angles];
        for (int a = 0; a < angles; a++)
        {
            int best = inner;
            for (int k = inner; k < samples; k++)
            {
                if (profile[a, k] < profile[a, best])
                {
                    best = k;
                }
            }

            rayMinimum[a] = profile[a, best];
            rayRadius[a] = best * step;
        }

        double annulusRadius = Median([.. rayRadius]);
        int core = Math.Max(2, (int)(0.45 * annulusRadius / step));
        double coreSum = 0;
        for (int a = 0; a < angles; a++)
        {
            for (int k = 0; k < Math.Min(core, samples); k++)
            {
                coreSum += profile[a, k];
            }
        }

        double meanRadius = rayRadius.Average();
        double ragged = Math.Sqrt(rayRadius.Average(r => (r - meanRadius) * (r - meanRadius)));
        return (paper, coreSum / (angles * Math.Min(core, samples)), PercentileOf([.. rayMinimum], 0.05), ragged);
    }

    /// <summary>Bilinear sampling with the border replicated, as OpenCV's remap samples it.</summary>
    private static double Replicate(GrayImage image, double x, double y)
    {
        x = Math.Clamp(x, 0, image.Width - 1);
        y = Math.Clamp(y, 0, image.Height - 1);
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y), x1 = Math.Min(x0 + 1, image.Width - 1), y1 = Math.Min(y0 + 1, image.Height - 1);
        double fx = x - x0, fy = y - y0;
        var p = image.Pixels;
        int w = image.Width;
        return ((1 - fy) * (((1 - fx) * p[(y0 * w) + x0]) + (fx * p[(y0 * w) + x1]))) + (fy * (((1 - fx) * p[(y1 * w) + x0]) + (fx * p[(y1 * w) + x1])));
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

    /// <summary>The q quantile of a byte image with linear interpolation between order statistics, numpy's default.</summary>
    private static double Percentile(byte[] values, double q)
    {
        var counts = new long[256];
        foreach (byte b in values)
        {
            counts[b]++;
        }

        double position = q * (values.Length - 1);
        long lower = (long)Math.Floor(position);
        double fraction = position - lower;
        int Value(long index)
        {
            long seen = 0;
            for (int i = 0; i < 256; i++)
            {
                seen += counts[i];
                if (seen > index)
                {
                    return i;
                }
            }

            return 255;
        }

        int low = Value(lower), high = Value(Math.Min(lower + 1, values.Length - 1));
        return low + (fraction * (high - low));
    }

    private static double PercentileOf(double[] values, double q)
    {
        var sorted = values.Order().ToArray();
        double position = q * (sorted.Length - 1);
        int lower = (int)Math.Floor(position);
        return lower + 1 < sorted.Length ? sorted[lower] + ((position - lower) * (sorted[lower + 1] - sorted[lower])) : sorted[lower];
    }

    private static double Median(IReadOnlyList<double> values)
    {
        var sorted = values.Where(v => !double.IsNaN(v)).Order().ToArray();
        int n = sorted.Length;
        return n == 0 ? double.NaN : n % 2 == 1 ? sorted[n / 2] : (sorted[(n / 2) - 1] + sorted[n / 2]) / 2;
    }
}
