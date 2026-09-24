using System.Globalization;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Measurement;

/// <summary>The two bull locators of PHASE0-SPIKE-BRIEF.md section 5.</summary>
public enum BullLocatorKind
{
    Centroid,
    EdgeFit,
}

/// <summary>A bull centre as declared and as recovered, in page dmm, with what the locator resolved on the way.</summary>
public sealed record BullLocation(
    int Index,
    string? Label,
    PointD Declared,
    PointD? Recovered,
    int Iterations,
    double? Threshold = null,
    double? InkSpread = null,
    int? EdgePoints = null,
    int? NearThresholdRays = null,
    string? Failure = null)
{
    public string Name => Label ?? Index.ToString(CultureInfo.InvariantCulture);

    public double Dx => Recovered is { } r ? r.X - Declared.X : double.NaN;

    public double Dy => Recovered is { } r ? r.Y - Declared.Y : double.NaN;

    public double Error => Math.Sqrt((Dx * Dx) + (Dy * Dy));
}

/// <summary>An inked band of a disc stack in dmm radii, TARGET-SCHEMA.md section 3.4. A solid disc has an inner radius of 0.</summary>
public sealed record InkBand(double Outer, double Inner);

public static class RingGeometry
{
    /// <summary>
    /// The inked bands of <paramref name="set"/>, outermost first. Section 3.4: disc i inks the ring between its radius
    /// and disc i+1's, adjacent inked discs make one band, and a paper disc lays nothing.
    /// </summary>
    public static IReadOnlyList<InkBand> Bands(TargetDefinition definition, RingSet set)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(set);
        var paper = definition.Inks.Where(i => i.Role == InkRole.Paper).Select(i => i.Key).ToHashSet(StringComparer.Ordinal);
        var bands = new List<InkBand>();
        for (int k = 0; k < set.Discs.Count;)
        {
            if (paper.Contains(set.Discs[k].Ink))
            {
                k++;
                continue;
            }

            int j = k;
            while (j + 1 < set.Discs.Count && !paper.Contains(set.Discs[j + 1].Ink))
            {
                j++;
            }

            bands.Add(new InkBand(set.Discs[k].Diameter / 2.0, j + 1 < set.Discs.Count ? set.Discs[j + 1].Diameter / 2.0 : 0));
            k = j + 1;
        }

        return bands;
    }

    /// <summary>
    /// The centroid mask radius: the middle of the paper band inside the outermost inked band. PHASE0-SPIKE-BRIEF.md
    /// section 5: the mask must hold the inner bands whole and clear the outer one, not merely contain it, or an estimate
    /// a few dmm out clips the outer ring and the asymmetry feeds the next iteration. 91.25 dmm on GL-CF25-LTR, against
    /// the 100 dmm the preliminary measurement used.
    /// </summary>
    public static double MaskRadius(IReadOnlyList<InkBand> bands)
    {
        ArgumentNullException.ThrowIfNull(bands);
        return bands.Count >= 2 ? (bands[0].Inner + bands[1].Outer) / 2 : bands.Count == 1 ? bands[0].Outer + 5 : 0;
    }
}

/// <summary>
/// PHASE0-SPIKE-BRIEF.md section 5's estimator, reproduced: an ink-weighted centroid over a circular mask on the page,
/// thresholded at the midpoint of the 3rd and 97th percentiles inside the mask, iterated until it stops moving. The
/// threshold is relative to the local range, as DETECTION-PIPELINE.md section 3 requires of every threshold; unthresholded,
/// the paper inside the mask outweighs the ink forty to one and illumination falloff moves the centroid by millimetres.
/// The centroid is taken in page coordinates, each pixel weighted by its page area, so it stays unbiased under perspective.
/// </summary>
public static class CentroidBullLocator
{
    public const int MaximumIterations = 30;

    /// <summary>
    /// Iteration stops when the estimate moves less than this, in dmm: 0.1 micrometre, 254 times inside conformance test
    /// 43's gate. A hundredth of that sat below the jitter of a photograph's numerical Jacobian and iterative inverse, so a
    /// correct estimate under strong lens distortion never settled.
    /// </summary>
    public const double Convergence = 0.001;

    public static BullLocation Locate(GrayImage image, IPageMapping map, int index, Bull bull, double maskRadius)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(bull);
        var declared = new PointD(bull.X, bull.Y);
        var centre = declared;
        var histogram = new int[256];
        double threshold = double.NaN;
        var pixels = image.Pixels;
        double limit = maskRadius * maskRadius;
        for (int iteration = 1; iteration <= MaximumIterations; iteration++)
        {
            if (Window.Bounds(image, map, centre, maskRadius) is not { } box)
            {
                return new BullLocation(index, bull.Label, declared, null, iteration, Failure: "the mask leaves the image");
            }

            var (low, high) = Window.Levels(image, map, box, centre, maskRadius, histogram);
            if (high - low < 1)
            {
                return new BullLocation(index, bull.Label, declared, null, iteration, Failure: "no ink contrast inside the mask");
            }

            threshold = (low + high) / 2;
            double sumW = 0, sumX = 0, sumY = 0;
            for (int v = box.Top; v <= box.Bottom; v++)
            {
                int row = v * image.Width;
                for (int u = box.Left; u <= box.Right; u++)
                {
                    byte value = pixels[row + u];
                    if (value >= threshold)
                    {
                        continue;
                    }

                    var at = new PointD(u, v);
                    var p = map.ToPage(at);
                    if (Math.Pow(p.X - centre.X, 2) + Math.Pow(p.Y - centre.Y, 2) > limit)
                    {
                        continue;
                    }

                    var j = map.Jacobian(at);
                    double weight = Math.Min(1, (threshold - value) / (threshold - low)) * Math.Abs((j.XX * j.YY) - (j.XY * j.YX));
                    sumW += weight;
                    sumX += weight * p.X;
                    sumY += weight * p.Y;
                }
            }

            if (sumW == 0)
            {
                return new BullLocation(index, bull.Label, declared, null, iteration, threshold, Failure: "no ink inside the mask");
            }

            var next = new PointD(sumX / sumW, sumY / sumW);
            double shift = Math.Sqrt(Math.Pow(next.X - centre.X, 2) + Math.Pow(next.Y - centre.Y, 2));
            centre = next;
            if (shift < Convergence)
            {
                return new BullLocation(index, bull.Label, declared, centre, iteration, threshold);
            }
        }

        return new BullLocation(index, bull.Label, declared, centre, MaximumIterations, threshold, Failure: "did not converge");
    }
}

/// <summary>
/// The edge fit PHASE0-SPIKE-BRIEF.md section 5 names as the next step past a centroid. Along 180 rays, every declared
/// band edge is located at the crossing midway between the ink and paper levels on either side of it; then a centre and a
/// single ink spread are fitted so that each outer edge of ink sits at its declared radius plus the spread and each inner
/// edge at its radius minus it. The spread is what keeps dot gain from biasing anything, and it comes out as a measurement.
/// Points more than four robust standard deviations from the fit are trimmed, and the rays are resampled about the new
/// centre until it stops moving. It locates an edge where the profile crosses midway between its own ink and paper
/// levels, so a change in ink density across a disc moves nothing, where a darkness-weighted centroid would follow it.
/// On the synthetic raster it recovers every bull to 0.00022 in worst at 300 DPI and 0.00013 at 600, a third and a half
/// of the centroid's figures, which is why it ships.
/// </summary>
public static class EdgeFitBullLocator
{
    public const int Rays = 180;

    public const int MaximumPasses = 8;

    /// <summary>How far each side of a declared edge a ray samples, in dmm, before the gap to the next edge shortens it.</summary>
    public const double MaximumHalfWidth = 3.0;

    /// <summary>
    /// The fewest samples an edge profile may have: <see cref="Crossing"/> averages three at each end for the ink and paper
    /// levels. NOTES-FROM-PLANNING.md entry 15 section 3: a mapping that gives fewer, as a degenerate registration does, fails
    /// that bull with a reason and the sheet carries on.
    /// </summary>
    public const int MinimumSamples = 3;

    /// <summary>
    /// The most samples an edge profile may have: 4096 across at most 6 dmm is 680 px per dmm, a thousand times a 600 DPI
    /// scan. More means the mapping has no usable scale at the bull, and sampling it would only exhaust memory.
    /// </summary>
    public const int MaximumSamples = 4096;

    /// <summary>Samples per image pixel along a ray.</summary>
    private const double SamplesPerPixel = 4;

    public static BullLocation Locate(GrayImage image, IPageMapping map, int index, Bull bull, IReadOnlyList<InkBand> bands) =>
        Converge(image, map, index, bull, bands).Location;

    /// <summary>
    /// How far a located bull's centre moves when any one of its edge points is left out, NOTES-FROM-PLANNING.md entry 52 section 3 and entry
    /// 49 section 5. The last pass's points are refitted from the same start with each point removed in turn. It also counts the points whose
    /// residual lies within a tenth of the fit's rejection limit, where a small change in the image flips a point in or out of the fit. A
    /// diagnostic: the locator's result is <see cref="Locate"/>'s, unchanged.
    /// </summary>
    public static EdgeFitSensitivity LeaveOneOut(GrayImage image, IPageMapping map, int index, Bull bull, IReadOnlyList<InkBand> bands)
    {
        var run = Converge(image, map, index, bull, bands);
        if (run.Location.Recovered is null || run.Observations is not { Count: > 12 } observations)
        {
            return new EdgeFitSensitivity(run.Location, run.Observations?.Count ?? 0, 0, null, null, 0);
        }

        var full = Fit(observations, run.Start, run.Spread);
        int near = full.Residuals.Count(r => Math.Abs(Math.Abs(r) - full.Limit) <= 0.1 * full.Limit);
        var shifts = new double[observations.Count];
        var without = new List<Observation>(observations.Count - 1);
        for (int i = 0; i < observations.Count; i++)
        {
            without.Clear();
            for (int k = 0; k < observations.Count; k++)
            {
                if (k != i)
                {
                    without.Add(observations[k]);
                }
            }

            var refit = Fit(without, run.Start, run.Spread);
            shifts[i] = Math.Sqrt(Math.Pow(refit.Centre.X - full.Centre.X, 2) + Math.Pow(refit.Centre.Y - full.Centre.Y, 2));
        }

        Array.Sort(shifts);
        return new EdgeFitSensitivity(run.Location, observations.Count, full.Used, shifts[^1], shifts[shifts.Length / 2], near);
    }

    private sealed record Converged(BullLocation Location, List<Observation>? Observations, PointD Start, double Spread);

    private static Converged Converge(GrayImage image, IPageMapping map, int index, Bull bull, IReadOnlyList<InkBand> bands)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(bull);
        ArgumentNullException.ThrowIfNull(bands);
        var declared = new PointD(bull.X, bull.Y);
        if (bands.Count == 0)
        {
            return new Converged(new BullLocation(index, bull.Label, declared, null, 0, Failure: "the ring set lays no ink"), null, declared, 0);
        }

        var edges = Edges(bands);
        var centre = declared;
        double spread = 0, reach = bands[0].Outer + MaximumHalfWidth + 1;
        int used = 0, near = 0;
        var histogram = new int[256];
        (List<Observation>? Observations, PointD Start, double Spread) last = (null, declared, 0);
        for (int pass = 1; pass <= MaximumPasses; pass++)
        {
            if (Window.Bounds(image, map, centre, reach) is not { } box)
            {
                return new Converged(new BullLocation(index, bull.Label, declared, null, pass, Failure: "the outer edge leaves the image"), null, declared, 0);
            }

            var (low, high) = Window.Levels(image, map, box, centre, reach, histogram);
            double contrast = high - low;
            if (contrast < 1)
            {
                return new Converged(new BullLocation(index, bull.Label, declared, null, pass, Failure: "no ink contrast at the bull"), null, declared, 0);
            }

            var j = map.Jacobian(map.ToImage(centre));
            double step = Math.Sqrt(Math.Abs((j.XX * j.YY) - (j.XY * j.YX))) / SamplesPerPixel;
            if (!double.IsFinite(step) || step <= 0)
            {
                return new Converged(new BullLocation(index, bull.Label, declared, null, pass, Failure: "the mapping has no finite scale at the bull"), null, declared, 0);
            }

            var observations = new List<Observation>(edges.Count * Rays);
            near = 0;
            foreach (var edge in edges)
            {
                double across = Math.Ceiling(2 * edge.HalfWidth / step) + 1;
                if (!(across >= MinimumSamples && across <= MaximumSamples))
                {
                    return new Converged(new BullLocation(index, bull.Label, declared, null, pass, Failure: string.Create(CultureInfo.InvariantCulture,
                        $"the mapping gives the {edge.Radius:0.#} dmm edge a profile of {across:0} samples at {step * SamplesPerPixel:0.###} dmm per pixel, outside {MinimumSamples} to {MaximumSamples}")), null, declared, 0);
                }

                int n = (int)across;
                var samples = new double[n];
                for (int k = 0; k < Rays; k++)
                {
                    double angle = 2 * Math.PI * k / Rays, cos = Math.Cos(angle), sin = Math.Sin(angle);
                    bool inside = true;
                    for (int s = 0; s < n && inside; s++)
                    {
                        double r = edge.Radius - edge.HalfWidth + (s * step);
                        var q = map.ToImage(new PointD(centre.X + (r * cos), centre.Y + (r * sin)));
                        samples[s] = ImageSampler.Bilinear(image, q.X, q.Y);
                        inside = !double.IsNaN(samples[s]);
                    }

                    if (!inside)
                    {
                        continue;
                    }

                    if (Crossing(samples, edge.Sign, contrast, out bool nearThreshold) is { } at)
                    {
                        double radius = edge.Radius - edge.HalfWidth + (at * step);
                        observations.Add(new Observation(centre.X + (radius * cos), centre.Y + (radius * sin), edge.Radius, edge.Sign));
                    }

                    if (nearThreshold)
                    {
                        near++;
                    }
                }
            }

            if (observations.Count < 12)
            {
                return new Converged(new BullLocation(index, bull.Label, declared, null, pass, Failure: $"{observations.Count} edge points found"), null, declared, 0);
            }

            var start = centre;
            double startSpread = spread;
            var (fitted, fittedSpread, count, _, _) = Fit(observations, centre, spread);
            if (!double.IsFinite(fitted.X) || !double.IsFinite(fitted.Y) || !double.IsFinite(fittedSpread))
            {
                return new Converged(new BullLocation(index, bull.Label, declared, null, pass, Failure: "the center fit diverged"), null, declared, 0);
            }

            double shift = Math.Sqrt(Math.Pow(fitted.X - centre.X, 2) + Math.Pow(fitted.Y - centre.Y, 2));
            centre = fitted;
            spread = fittedSpread;
            used = count;
            last = (observations, start, startSpread);
            if (shift < CentroidBullLocator.Convergence)
            {
                return new Converged(new BullLocation(index, bull.Label, declared, centre, pass, null, spread, used, near), observations, start, startSpread);
            }
        }

        return new Converged(new BullLocation(index, bull.Label, declared, centre, MaximumPasses, null, spread, used, near, "did not converge"), last.Observations, last.Start, last.Spread);
    }

    private readonly record struct Edge(double Radius, int Sign, double HalfWidth);

    private readonly record struct Observation(double X, double Y, double Radius, int Sign);

    /// <summary>Every edge of every band, outermost first; sign +1 where the ink is inside the edge and -1 where it is outside.</summary>
    private static List<Edge> Edges(IReadOnlyList<InkBand> bands)
    {
        var radii = new List<(double Radius, int Sign)>();
        foreach (var band in bands)
        {
            radii.Add((band.Outer, 1));
            if (band.Inner > 0)
            {
                radii.Add((band.Inner, -1));
            }
        }

        var edges = new List<Edge>(radii.Count);
        for (int i = 0; i < radii.Count; i++)
        {
            double gapOut = i == 0 ? double.PositiveInfinity : radii[i - 1].Radius - radii[i].Radius;
            double gapIn = i == radii.Count - 1 ? radii[i].Radius : radii[i].Radius - radii[i + 1].Radius;
            edges.Add(new Edge(radii[i].Radius, radii[i].Sign, Math.Min(MaximumHalfWidth, 0.45 * Math.Min(gapOut, gapIn))));
        }

        return edges;
    }

    /// <summary>
    /// The sample index where the profile crosses midway between its two ends, nearest the declared edge. A profile whose
    /// ends differ by less than half the bull's ink-to-paper range, or in the wrong direction, has no edge to report.
    /// <para>
    /// <paramref name="nearThreshold"/> is set when that rise lies within a tenth of the half-contrast threshold, on either
    /// side of it, NOTES-FROM-PLANNING.md entry 55 section 3 item 1. Entry 52 section 3 found that this test, rather than the
    /// fit's rejection, decides how many points a bull rests on, so a ray this close to it is one the image could flip either
    /// way. It is counted and reported; nothing here depends on it.
    /// </para>
    /// </summary>
    private static double? Crossing(double[] v, int sign, double contrast, out bool nearThreshold)
    {
        nearThreshold = false;
        int n = v.Length, end = Math.Max(3, n / 8);
        if (n < MinimumSamples)
        {
            return null;
        }

        double inner = 0, outer = 0;
        for (int i = 0; i < end; i++)
        {
            inner += v[i];
            outer += v[n - 1 - i];
        }

        inner /= end;
        outer /= end;
        double rise = sign > 0 ? outer - inner : inner - outer;
        double threshold = 0.5 * contrast;
        nearThreshold = Math.Abs(rise - threshold) <= 0.1 * threshold;
        if (rise < threshold)
        {
            return null;
        }

        double mid = (inner + outer) / 2, centre = (n - 1) / 2.0;
        double? best = null;
        for (int s = 0; s + 1 < n; s++)
        {
            double a = v[s] - mid, b = v[s + 1] - mid;
            if ((a < 0 && b < 0) || (a > 0 && b > 0) || a == b)
            {
                continue;
            }

            double at = s + (a / (a - b));
            if (best is null || Math.Abs(at - centre) < Math.Abs(best.Value - centre))
            {
                best = at;
            }
        }

        return best;
    }

    private static (PointD Centre, double Spread, int Used, double[] Residuals, double Limit) Fit(List<Observation> observations, PointD start, double spread)
    {
        double cx = start.X, cy = start.Y, g = spread;
        var active = new bool[observations.Count];
        Array.Fill(active, true);
        var residuals = new double[observations.Count];
        double lastLimit = double.NaN;
        for (int round = 0; round < 3; round++)
        {
            for (int iteration = 0; iteration < 20; iteration++)
            {
                var normal = new double[3, 3];
                var gradient = new double[3];
                for (int i = 0; i < observations.Count; i++)
                {
                    if (!active[i])
                    {
                        continue;
                    }

                    var o = observations[i];
                    double dx = o.X - cx, dy = o.Y - cy, d = Math.Sqrt((dx * dx) + (dy * dy));
                    if (d == 0)
                    {
                        continue;
                    }

                    double r = d - o.Radius - (o.Sign * g);
                    double[] jr = [-dx / d, -dy / d, -o.Sign];
                    for (int a = 0; a < 3; a++)
                    {
                        gradient[a] -= jr[a] * r;
                        for (int b = 0; b < 3; b++)
                        {
                            normal[a, b] += jr[a] * jr[b];
                        }
                    }
                }

                if (LinearSolve.Solve(normal, gradient) is not { } delta)
                {
                    break;
                }

                cx += delta[0];
                cy += delta[1];
                g += delta[2];
                if (Math.Abs(delta[0]) + Math.Abs(delta[1]) < 1e-7)
                {
                    break;
                }
            }

            for (int i = 0; i < observations.Count; i++)
            {
                var o = observations[i];
                residuals[i] = Math.Sqrt(Math.Pow(o.X - cx, 2) + Math.Pow(o.Y - cy, 2)) - o.Radius - (o.Sign * g);
            }

            var absolute = residuals.Where((_, i) => active[i]).Select(Math.Abs).Order().ToArray();
            if (absolute.Length == 0)
            {
                break;
            }

            double limit = Math.Max(4 * 1.4826 * absolute[absolute.Length / 2], 0.01);
            lastLimit = limit;
            for (int i = 0; i < observations.Count; i++)
            {
                active[i] = Math.Abs(residuals[i]) <= limit;
            }
        }

        return (new PointD(cx, cy), g, active.Count(a => a), residuals, lastLimit);
    }
}

/// <summary>
/// The edge fit's sensitivity to its own points, NOTES-FROM-PLANNING.md entry 52 section 3: the location, how many edge points the last pass
/// found and how many the fit used, the largest and median movement of the centre in dmm when one point is left out, and how many points lie
/// within a tenth of the rejection limit. The shifts are null when the bull was not located.
/// </summary>
public sealed record EdgeFitSensitivity(BullLocation Location, int Observations, int Used, double? LargestShift, double? MedianShift, int NearLimit);

internal readonly record struct PixelBox(int Left, int Top, int Right, int Bottom);

internal static class Window
{
    /// <summary>The pixel box holding the page circle, or null when any of it falls outside the image.</summary>
    public static PixelBox? Bounds(GrayImage image, IPageMapping map, PointD centre, double radius)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        for (int k = 0; k < 32; k++)
        {
            double angle = 2 * Math.PI * k / 32;
            var q = map.ToImage(new PointD(centre.X + (radius * Math.Cos(angle)), centre.Y + (radius * Math.Sin(angle))));
            minX = Math.Min(minX, q.X);
            minY = Math.Min(minY, q.Y);
            maxX = Math.Max(maxX, q.X);
            maxY = Math.Max(maxY, q.Y);
        }

        int left = (int)Math.Floor(minX) - 2, top = (int)Math.Floor(minY) - 2;
        int right = (int)Math.Ceiling(maxX) + 2, bottom = (int)Math.Ceiling(maxY) + 2;
        return left < 0 || top < 0 || right >= image.Width || bottom >= image.Height ? null : new PixelBox(left, top, right, bottom);
    }

    /// <summary>The 3rd and 97th percentile grey levels of the pixels inside the page circle.</summary>
    public static (double Low, double High) Levels(GrayImage image, IPageMapping map, PixelBox box, PointD centre, double radius, int[] histogram)
    {
        Array.Clear(histogram);
        int count = 0;
        double limit = radius * radius;
        for (int v = box.Top; v <= box.Bottom; v++)
        {
            int row = v * image.Width;
            for (int u = box.Left; u <= box.Right; u++)
            {
                var p = map.ToPage(new PointD(u, v));
                if (Math.Pow(p.X - centre.X, 2) + Math.Pow(p.Y - centre.Y, 2) <= limit)
                {
                    histogram[image.Pixels[row + u]]++;
                    count++;
                }
            }
        }

        return (Percentile(histogram, count, 0.03), Percentile(histogram, count, 0.97));
    }

    private static double Percentile(int[] histogram, int count, double q)
    {
        long target = Math.Max(1, (long)Math.Ceiling(q * count)), seen = 0;
        for (int i = 0; i < histogram.Length; i++)
        {
            seen += histogram[i];
            if (seen >= target)
            {
                return i;
            }
        }

        return 255;
    }
}

internal static class ImageSampler
{
    /// <summary>The bilinear grey level at (x, y), with pixel (u, v) at (u, v); NaN outside the image.</summary>
    public static double Bilinear(GrayImage image, double x, double y)
    {
        if (!(x >= 0 && y >= 0 && x <= image.Width - 1 && y <= image.Height - 1))
        {
            return double.NaN;
        }

        int x0 = (int)x, y0 = (int)y, w = image.Width;
        int x1 = Math.Min(x0 + 1, w - 1), y1 = Math.Min(y0 + 1, image.Height - 1);
        double fx = x - x0, fy = y - y0;
        var p = image.Pixels;
        return ((1 - fy) * (((1 - fx) * p[(y0 * w) + x0]) + (fx * p[(y0 * w) + x1])))
            + (fy * (((1 - fx) * p[(y1 * w) + x0]) + (fx * p[(y1 * w) + x1])));
    }
}
