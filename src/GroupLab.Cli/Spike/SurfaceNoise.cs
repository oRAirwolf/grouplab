using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Cli.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 16 section 5: before a general developable surface is built, whether the mounted frames'
/// corners are good enough for any surface model to pass. Each frame's post-fit corner residual in
/// <c>scans/phase0/measurements/surface.json</c> is converted to image pixels per axis, at each marker's own scale in the
/// image (its mean edge in pixels over its size in dmm), and placed on the noise sweep of PHASE1-RESULTS.md M1.7 by
/// computing the same statistics on synthetic trials whose corner noise is known, the M1.7 truth camera at a 0.40 in bow
/// with every marker and the lens free.
/// <para>
/// Three statistics, because the fit's 2.54 dmm reclassification truncates the residual of the corners it keeps: RMS over
/// the kept corners, RMS over all of them, and a robust sigma, the median two-dimensional residual over the square root of
/// 2 ln 2, which is the per-axis sigma of a two-dimensional Gaussian and ignores the corners a misfit throws far out. A
/// frame is placed on the sweep by the synthetic noise whose median robust sigma matches its own, in pixels and, because
/// the gate is on the page and the frames' scales differ, in page dmm at the sweep's scale.
/// </para>
/// </summary>
public static class SurfaceNoise
{
    private const int Width = 3000, Height = 4000, Seeds = 10;
    private const double Gate = 1.27;
    private static readonly double[] Noises = [0.5, 1.0, 1.5, 2.0, 3.0, 5.0];
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    internal sealed record Corner(int Marker, PointD Image, double ErrorDmm, bool Kept);

    internal sealed record Statistics(int Corners, int Kept, double PixelsPerDmm, double RmsKeptPx, double RmsAllPx, double RobustPx)
    {
        public double RobustDmm => RobustPx / PixelsPerDmm;
    }

    private sealed record Calibration(double NoisePx, int Seed, Statistics Statistics, double WorstBullIn);

    public static int Run(string scans, string frozenDirectory, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var definition = Phase0Spike.Definition(frozenDirectory, SampleSet.CentreFire);
        double w = definition.Page.Width, h = definition.Page.Height;
        var truth = SurfaceLens.Bow(0.40, w, h);

        var specs = Noises.SelectMany((n, ni) => Enumerable.Range(1, Seeds).Select(seed => (Noise: n, Seed: seed, Stream: (seed * 7919) + (ni * 104729)))).ToList();
        var trials = new Calibration[specs.Count];
        Parallel.For(0, specs.Count, i =>
        {
            var s = specs[i];
            var matches = SyntheticSurface.Corners(definition, truth, 0, s.Noise, new Random(s.Stream), Width, Height);
            var image = matches.SelectMany(m => m.ImageCorners).ToList();
            var page = matches.SelectMany(m => m.PageCorners).ToList();
            var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
            var frame = new SurfaceFrame("noise", image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, 4000 * 23 / 36.0, w / 2, h / 2), 0, 0, w, h);
            var fit = SurfaceFit.Fit([frame], shareCamera: false)[0];
            var statistics = Measure([.. image.Select((q, k) => new Corner(k / 4, q, fit.PageErrors[k], fit.Kept[k]))], definition.Fiducials!.MarkerSize);
            double worst = definition.Bulls.Max(b =>
            {
                var declared = new PointD(b.X, b.Y);
                return Dist(fit.Mapping.ToPage(SyntheticSurface.Image(truth, 0, w, h, declared)), declared);
            });
            trials[i] = new Calibration(s.Noise, s.Seed, statistics, worst / 254);
        });

        var levels = trials.GroupBy(t => t.NoisePx).Select(g => (Noise: g.Key, Robust: Median(g.Select(t => t.Statistics.RobustPx)), Scale: Median(g.Select(t => t.Statistics.PixelsPerDmm)), Trials: g.ToList())).OrderBy(l => l.Noise).ToList();
        double sweepScale = Median(trials.Select(t => t.Statistics.PixelsPerDmm));

        output.WriteLine(string.Create(Inv, $"The sweep, as M1.7's truth camera at a 0.40 in bow, every marker, lens free, {Seeds} seeds per level, {sweepScale:0.00} px per dmm. Medians, and the 90th percentile of the worst bull, inches."));
        output.WriteLine();
        output.WriteLine("| Corner noise put in (px per axis) | Corners kept | Post-fit RMS, kept corners (px per axis) | Post-fit RMS, all corners (px per axis) | Robust post-fit sigma (px per axis) | Surface worst bull, median / 90th pct | Gate |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        foreach (var level in levels)
        {
            var t = level.Trials;
            double median = Percentile(t.Select(x => x.WorstBullIn), 0.5), p90 = Percentile(t.Select(x => x.WorstBullIn), 0.9);
            output.WriteLine(string.Create(Inv,
                $"| {level.Noise:0.0} | {Median(t.Select(x => (double)x.Statistics.Kept)):0} of {t[0].Statistics.Corners} | {Median(t.Select(x => x.Statistics.RmsKeptPx)):0.00} | {Median(t.Select(x => x.Statistics.RmsAllPx)):0.00} | {level.Robust:0.00} | {median:0.00000} / {p90:0.00000} | {(p90 < Gate / 254 ? "pass" : median < Gate / 254 ? "median passes, 90th fails" : "fail")} |"));
        }

        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(scans, "measurements", "surface.json")));
        var frames = new List<object>();
        output.WriteLine();
        output.WriteLine("Every gated photograph, from the M1.5 fit in surface.json. Equivalent sweep noise: the synthetic corner noise whose median robust sigma matches the frame's, by pixels, and by page dmm at the sweep's scale. Worst scoring bull through the surface, inches.");
        output.WriteLine();
        output.WriteLine("| Gate | Photograph | Markers | Scale (px per dmm) | Corners kept | Post-fit RMS, kept (px per axis) | Post-fit RMS, all (px per axis) | Robust sigma (px per axis) | Robust sigma (dmm per axis) | Equivalent sweep noise, by px / by dmm | Worst scoring bull |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var row in document.RootElement.GetProperty("rows").EnumerateArray())
        {
            string gate = row.GetProperty("gate").GetString()!;
            if (gate is not ("mounted" or "flat") || !row.TryGetProperty("corners", out var cornerRows))
            {
                continue;
            }

            string file = row.GetProperty("file").GetString()!;
            var sample = SampleSet.All.Single(s => s.File == file);
            var frameDefinition = Phase0Spike.Definition(frozenDirectory, sample.Definition);
            var corners = cornerRows.EnumerateArray().Select((c, i) => new Corner(i / 4,
                new PointD(c.GetProperty("imageXPx").GetDouble(), c.GetProperty("imageYPx").GetDouble()),
                c.GetProperty("error").TryGetDouble(out double e) ? e : double.NaN, c.GetProperty("kept").GetBoolean())).ToList();
            var statistics = Measure(corners, frameDefinition.Fiducials!.MarkerSize);
            double worst = row.GetProperty("bulls").GetProperty("surface").EnumerateArray()
                .Where(b => b.GetProperty("scoring").GetBoolean() && b.GetProperty("recoveredX").ValueKind == JsonValueKind.Number)
                .Select(b => b.GetProperty("error").GetDouble()).DefaultIfEmpty(double.NaN).Max() / 254;
            double byPixels = Equivalent(statistics.RobustPx, levels), byDmm = Equivalent(statistics.RobustDmm * sweepScale, levels);
            output.WriteLine(string.Create(Inv,
                $"| {gate} | `{file}` | {corners.Count / 4} | {statistics.PixelsPerDmm:0.00} | {statistics.Kept} of {statistics.Corners} | {statistics.RmsKeptPx:0.00} | {statistics.RmsAllPx:0.00} | {statistics.RobustPx:0.00} | {statistics.RobustDmm:0.00} | {Show(byPixels)} / {Show(byDmm)} | {worst:0.00000} |"));
            frames.Add(new { file, gate, markers = corners.Count / 4, statistics, statistics.RobustDmm, equivalentNoisePx = new { byPixels, byDmm }, worstScoringBullIn = worst });
        }

        RawMeasurements.Write(scans, "surface-noise", new { calibration = trials, frames });
        return 0;
    }

    /// <summary>A frame's corner statistics: each corner's page residual scaled to pixels by its own marker's size in the image.</summary>
    internal static Statistics Measure(IReadOnlyList<Corner> corners, double markerSize)
    {
        ArgumentNullException.ThrowIfNull(corners);
        int markers = corners.Count / 4;
        var scale = new double[markers];
        for (int m = 0; m < markers; m++)
        {
            double edges = 0;
            for (int k = 0; k < 4; k++)
            {
                edges += Dist(corners[(4 * m) + k].Image, corners[(4 * m) + ((k + 1) % 4)].Image);
            }

            scale[m] = edges / 4 / markerSize;
        }

        var pixels = corners.Select((c, i) => (Px: c.ErrorDmm * scale[i / 4], c.Kept)).Where(c => double.IsFinite(c.Px)).ToList();
        double Rms(IEnumerable<double> values) => Math.Sqrt(values.Select(v => v * v).DefaultIfEmpty(double.NaN).Average() / 2);
        return new Statistics(corners.Count, corners.Count(c => c.Kept), Median(scale), Rms(pixels.Where(p => p.Kept).Select(p => p.Px)), Rms(pixels.Select(p => p.Px)),
            Median(pixels.Select(p => p.Px)) / Math.Sqrt(2 * Math.Log(2)));
    }

    /// <summary>The synthetic noise, by linear interpolation, whose median robust sigma is <paramref name="robust"/>; infinity beyond the sweep.</summary>
    private static double Equivalent(double robust, IReadOnlyList<(double Noise, double Robust, double Scale, List<Calibration> Trials)> levels)
    {
        if (robust <= levels[0].Robust)
        {
            return levels[0].Noise * robust / levels[0].Robust;
        }

        for (int i = 1; i < levels.Count; i++)
        {
            if (robust <= levels[i].Robust)
            {
                return levels[i - 1].Noise + ((levels[i].Noise - levels[i - 1].Noise) * (robust - levels[i - 1].Robust) / (levels[i].Robust - levels[i - 1].Robust));
            }
        }

        return double.PositiveInfinity;
    }

    private static string Show(double noise) => double.IsPositiveInfinity(noise) ? "above 5" : noise.ToString("0.00", Inv);

    private static double Median(IEnumerable<double> values) => Percentile(values, 0.5);

    private static double Percentile(IEnumerable<double> values, double q)
    {
        var sorted = values.Where(v => !double.IsNaN(v)).Order().ToList();
        return sorted.Count == 0 ? double.NaN : sorted[Math.Min(sorted.Count - 1, (int)Math.Floor((q * (sorted.Count - 1)) + 0.5))];
    }

    private static double Dist(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
