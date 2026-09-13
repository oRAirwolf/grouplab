using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// The measurements of PHASE0-SPIKE-BRIEF.md section 6, numbered as there, and the photograph table of section 7, each
/// printed as a Markdown table over the committed sample set, with its rows written by <see cref="RawMeasurements"/>.
/// All reported lengths are in inches.
/// </summary>
public static class Phase0Measurements
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly OpenCvSharpBackend Backend = new();

    private sealed record Run(SampleSet.Sample Sample, GrayImage Image, ImageMetadata Metadata, TargetDefinition Definition, FiducialResult Fiducials, RegistrationFit? Fit, IReadOnlyList<BullLocation> Bulls, long DetectMs);

    /// <summary>
    /// Section 7 and NOTES-FROM-PLANNING.md entry 6: every photograph through the shipped pipeline, grouped by lens, the lens
    /// named by its focal length and f-number, with the worst scoring bull and the worst sighter called out separately.
    /// A photograph whose sheet overflows the frame is listed as excluded rather than measured against the gate.
    /// </summary>
    public static int Photos(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        output.WriteLine("| Lens | Photograph | Markers | Residual RMS / max (in) | Homography alone, RMS (in) | Lens k1 / k2 | Distortion at the frame edge | Bull mean (in) | Worst scoring bull (in) | Worst sighter (in) | Photograph gate |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|");
        var definition = Phase0Spike.Definition(targets, SampleSet.CentreFire);
        var results = SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Photograph)
            .Select(s => Phase0Spike.Measure(scans, targets, s, Backend, new MeasureOptions())).ToList();
        foreach (var r in results.OrderBy(r => r.Metadata.FocalLengthMm).ThenBy(r => r.Metadata.FNumber).ThenBy(r => r.Sample.File, StringComparer.Ordinal))
        {
            string lens = r.Metadata.FocalLengthMm is { } f ? string.Create(Inv, $"{f:0.00} mm f/{r.Metadata.FNumber:0.0}") : "unknown";
            string markers = $"{r.Fiducials.Matches.Count}/{r.Fiducials.Expected}";
            if (r.Sample.Excluded is { } why)
            {
                output.WriteLine($"| {lens} | `{r.Sample.File}` | {markers} | excluded: {why} | | | | | | | excluded |");
                continue;
            }

            if (r.Fit is null)
            {
                output.WriteLine($"| {lens} | `{r.Sample.File}` | {markers} | {r.Failure} | | | | | | | fail |");
                continue;
            }

            var located = r.EdgeFit.Where(b => b.Recovered is not null).ToList();
            var scoring = located.Where(b => definition.Bulls[b.Index].Scoring).MaxBy(b => b.Error);
            var sighter = located.Where(b => !definition.Bulls[b.Index].Scoring).MaxBy(b => b.Error);
            var e = Phase0Spike.Stats(r.EdgeFit);
            string terms = r.Lens is { } l ? string.Create(Inv, $"{l.K1:+0.0000;-0.0000} / {l.K2:+0.0000;-0.0000}") : "";
            string edge = r.Lens is { } d ? string.Create(Inv, $"{d.FrameEdgePixels:0} px = {d.FrameEdgeInches:0.000} in") : "";
            output.WriteLine(string.Create(Inv,
                $"| {lens} | `{r.Sample.File}` | {markers} | {In(r.Fit.RmsResidual)} / {In(r.Fit.MaxResidual)} | {In(r.Fit.HomographyRmsResidual ?? double.NaN)} | {terms} | {edge} | {In(e.Mean)} | {In(scoring?.Error ?? double.NaN)} at {scoring?.Name} | {In(sighter?.Error ?? double.NaN)} at {sighter?.Name} | {Verdict(e)} |"));
        }

        RawMeasurements.Write(scans, "photos", results.Select(r => RawMeasurements.Sample(r, definition)).ToList());
        return 0;
    }

    /// <summary>
    /// Measurement 1: residual and worst bull against marker count. Random subsets of the matched markers are refitted by
    /// homography; bull centres are located once with every marker and mapped through each subset's fit, so the table
    /// measures the registration and not the locator. The residual is over the corners of every matched marker, held-out
    /// ones included. The four real tiles, which carry nine markers each, follow.
    /// </summary>
    public static int MarkerCount(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        int[] counts = [4, 6, 9, 12, 16, 20, 25, 30, 34];
        const int Trials = 40;
        var random = new Random(20260913);
        var raw = new List<object>();
        output.WriteLine("| Sheet | DPI | Markers used | Fits | Residual RMS, median / 90th percentile (in) | Worst bull, median / 90th percentile (in) | Mean bull, median (in) |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        foreach (var sample in SampleSet.All.Where(IsReferenceSheet))
        {
            var run = Measure(scans, targets, sample, new MeasureOptions(), BullLocatorKind.EdgeFit);
            if (run.Fit is null)
            {
                continue;
            }

            var bulls = run.Bulls.Where(b => b.Recovered is not null).ToList();
            var bullImage = bulls.Select(b => run.Fit.Mapping.ToImage(b.Recovered!.Value)).ToList();
            var allImage = run.Fiducials.Matches.SelectMany(m => m.ImageCorners).ToList();
            var allPage = run.Fiducials.Matches.SelectMany(m => m.PageCorners).ToList();
            int[] ids = [.. run.Fiducials.Matches.Select(m => m.Id)];
            var trialRows = new List<object>();
            foreach (int count in counts.Where(c => c <= ids.Length))
            {
                var residuals = new List<double>();
                var worsts = new List<double>();
                var means = new List<double>();
                int trials = count == ids.Length ? 1 : Trials;
                for (int t = 0; t < trials; t++)
                {
                    var subset = ids.OrderBy(_ => random.Next()).Take(count).ToHashSet();
                    var fit = SheetMeasurer.Register(run.Image, run.Metadata, run.Fiducials, new MeasureOptions(), Backend, new TraceRecorder(), subset);
                    if (fit is null)
                    {
                        continue;
                    }

                    double residual = Math.Sqrt(allImage.Select((p, i) => Squared(fit.Mapping.ToPage(p), allPage[i])).Average());
                    var errors = bullImage.Select((p, i) => Math.Sqrt(Squared(fit.Mapping.ToPage(p), bulls[i].Declared))).ToList();
                    residuals.Add(residual);
                    worsts.Add(errors.Max());
                    means.Add(errors.Average());
                    trialRows.Add(new
                    {
                        count,
                        subset = subset.Order().ToArray(),
                        residualRms = RawMeasurements.R(residual),
                        bulls = bulls.Select((b, i) => new { label = b.Name, error = RawMeasurements.R(errors[i]) }).ToArray(),
                    });
                }

                output.WriteLine(string.Create(Inv,
                    $"| {sample.Description} | {sample.Dpi} | {count} of {ids.Length} | {residuals.Count} | {In(Percentile(residuals, 0.5))} / {In(Percentile(residuals, 0.9))} | {In(Percentile(worsts, 0.5))} / {In(Percentile(worsts, 0.9))} | {In(Percentile(means, 0.5))} |"));
            }

            raw.Add(new { file = sample.File, dpi = sample.Dpi, markers = ids.Order().ToArray(), seed = 20260913, trials = trialRows });
        }

        foreach (var sample in SampleSet.All.Where(s => s.Definition == SampleSet.Tile && s.Kind == SampleSet.SampleKind.Scan))
        {
            var run = Measure(scans, targets, sample, new MeasureOptions(), BullLocatorKind.EdgeFit);
            var stats = Phase0Spike.Stats(run.Bulls);
            output.WriteLine(string.Create(Inv,
                $"| GL-LR300-T {sample.Description} | {sample.Dpi} | {run.Fiducials.Matches.Count} of {run.Fiducials.Expected}, as printed | 1 | {In(run.Fit?.RmsResidual ?? double.NaN)} | {In(stats.Worst)} | {In(stats.Mean)} |"));
            raw.Add(new
            {
                file = sample.File,
                dpi = sample.Dpi,
                markers = run.Fiducials.Matches.Select(m => m.Id).Order().ToArray(),
                residualRms = RawMeasurements.R(run.Fit?.RmsResidual ?? double.NaN),
                mapping = RawMeasurements.Mapping(run.Fit?.Mapping),
                bulls = RawMeasurements.Bulls(run.Bulls, run.Definition),
            });
        }

        RawMeasurements.Write(scans, "markers", raw);
        return 0;
    }

    /// <summary>
    /// Measurement 2: corner refinement on paper and on the synthetic raster, NONE, CONTOUR and SUBPIX with a window
    /// sweep. On the synthetic raster truth is known, so corner error is measured against it directly, with its radial
    /// component about the marker centre separated out as a bias.
    /// </summary>
    public static int Refinement(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        (string Name, CornerRefinement Method, double? Window)[] variants =
        [
            ("none", CornerRefinement.None, null),
            ("contour", CornerRefinement.Contour, null),
            ("subpix, shipped window", CornerRefinement.Subpixel, null),
            ("subpix, 0.25 module", CornerRefinement.Subpixel, 0.25),
            ("subpix, 0.5 module", CornerRefinement.Subpixel, 0.5),
            ("subpix, 1 module", CornerRefinement.Subpixel, 1.0),
            ("subpix, 1.5 modules", CornerRefinement.Subpixel, 1.5),
            ("subpix, 2 modules", CornerRefinement.Subpixel, 2.0),
        ];
        var paperRows = new List<object>();
        var syntheticRows = new List<object>();

        output.WriteLine("Paper: sheets 1 to 3 of GL-CF25-LTR. Residual and bull figures are the mean over the three sheets; the worst bull is the worst on any of them.");
        output.WriteLine();
        output.WriteLine("| DPI | Refinement | Markers matched | Residual RMS (in) | Residual max (in) | Bull mean (in) | Worst bull (in) |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        foreach (int dpi in (int[])[600, 300])
        {
            var loaded = SampleSet.All.Where(s => IsReferenceSheet(s) && s.Dpi == dpi)
                .Select(s => (Sample: s, Loaded: ImageLoader.Load(Path.Combine(scans, s.File)))).ToList();
            foreach (var (name, method, window) in variants)
            {
                var options = new MeasureOptions(Refinement: method, RefinementWindowModules: window);
                var runs = loaded.Select(l => Measure(l.Loaded.Image, l.Loaded.Metadata, Phase0Spike.Definition(targets, l.Sample.Definition), l.Sample, options, BullLocatorKind.EdgeFit)).ToList();
                var stats = runs.Select(r => Phase0Spike.Stats(r.Bulls)).ToList();
                output.WriteLine(string.Create(Inv,
                    $"| {dpi} | {name} | {runs.Sum(r => r.Fiducials.Matches.Count)} of {runs.Sum(r => r.Fiducials.Expected)} | {In(runs.Average(r => r.Fit?.RmsResidual ?? double.NaN))} | {In(runs.Average(r => r.Fit?.MaxResidual ?? double.NaN))} | {In(stats.Average(s => s.Mean))} | {In(stats.Max(s => s.Worst))} |"));
                paperRows.AddRange(runs.Select(r => (object)new
                {
                    dpi,
                    refinement = name,
                    file = r.Sample.File,
                    markersMatched = r.Fiducials.Matches.Count,
                    residualRms = RawMeasurements.R(r.Fit?.RmsResidual ?? double.NaN),
                    residualMax = RawMeasurements.R(r.Fit?.MaxResidual ?? double.NaN),
                    corners = r.Fit is { } f ? RawMeasurements.Corners(f) : null,
                    bulls = RawMeasurements.Bulls(r.Bulls, r.Definition),
                }));
            }
        }

        output.WriteLine();
        output.WriteLine("Synthetic: GL-CF25-LTR rendered by SceneRasterizer and distorted by the test 43 perturbation, so every corner's true position is known.");
        output.WriteLine();
        output.WriteLine("| DPI | Refinement | Markers matched | Corner error against truth, RMS (px) | Mean radial bias (px, + outward) | Residual RMS (in) | Worst bull (in) |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        var definition = Phase0Spike.Definition(targets, SampleSet.CentreFire);
        var page = SceneBuilder.Build(definition).Pages[0];
        foreach (int dpi in (int[])[600, 300])
        {
            var render = SceneRasterizer.Rasterize(page, dpi);
            var (transform, width, height) = Perturbation.Phase0.For(render.Width, render.Height, dpi);
            var scan = Backend.WarpPerspective(render, transform, width, height);
            var metadata = ImageMetadata.ForScan(width, height, dpi);
            var sample = new SampleSet.Sample("synthetic", SampleSet.CentreFire, 0, dpi, SampleSet.SampleKind.Scan, false, "synthetic");
            foreach (var (name, method, window) in variants)
            {
                var run = Measure(scan, metadata, definition, sample, new MeasureOptions(Refinement: method, RefinementWindowModules: window), BullLocatorKind.EdgeFit);
                double sum = 0, bias = 0;
                int n = 0;
                var cornerRows = new List<object>();
                foreach (var match in run.Fiducials.Matches)
                {
                    var truth = match.PageCorners.Select(p => transform.Apply(new PointD((p.X * dpi / 254) - 0.5, (p.Y * dpi / 254) - 0.5))).ToList();
                    var centre = new PointD(truth.Average(p => p.X), truth.Average(p => p.Y));
                    for (int k = 0; k < 4; k++)
                    {
                        double ex = match.ImageCorners[k].X - truth[k].X, ey = match.ImageCorners[k].Y - truth[k].Y;
                        double rx = truth[k].X - centre.X, ry = truth[k].Y - centre.Y, length = Math.Sqrt((rx * rx) + (ry * ry));
                        double radial = ((ex * rx) + (ey * ry)) / length;
                        sum += (ex * ex) + (ey * ey);
                        bias += radial;
                        n++;
                        cornerRows.Add(new
                        {
                            markerId = match.Id,
                            corner = k,
                            truthXPx = RawMeasurements.R(truth[k].X),
                            truthYPx = RawMeasurements.R(truth[k].Y),
                            errorXPx = RawMeasurements.R(ex),
                            errorYPx = RawMeasurements.R(ey),
                            radialPx = RawMeasurements.R(radial),
                        });
                    }
                }

                var stats = Phase0Spike.Stats(run.Bulls);
                output.WriteLine(string.Create(Inv,
                    $"| {dpi} | {name} | {run.Fiducials.Matches.Count} of {run.Fiducials.Expected} | {Math.Sqrt(sum / n):0.000} | {bias / n:+0.000;-0.000} | {In(run.Fit?.RmsResidual ?? double.NaN)} | {In(stats.Worst)} |"));
                syntheticRows.Add(new
                {
                    dpi,
                    refinement = name,
                    perturbation = Perturbation.Phase0,
                    residualRms = RawMeasurements.R(run.Fit?.RmsResidual ?? double.NaN),
                    corners = cornerRows,
                    bulls = RawMeasurements.Bulls(run.Bulls, definition),
                });
            }
        }

        RawMeasurements.Write(scans, "refinement", new { paper = paperRows, synthetic = syntheticRows });
        return 0;
    }

    /// <summary>
    /// Measurement 3: the adaptive threshold window on 600 DPI input, and downsampling before detection, over the 600 DPI
    /// scans of all ten sheets, against the native 300 DPI scans of the same sheets.
    /// </summary>
    public static int Threshold(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        (string Window, int? Max, int Downsample)[] variants =
        [
            ("7 px", 7, 1), ("15 px", 15, 1), ("23 px, OpenCV's default", 23, 1), ("35 px", 35, 1),
            ("half the marker side, 49 px", 49, 1), ("the marker side, 95 px, shipped", null, 1), ("71 px", 71, 1),
            ("the marker side", null, 2), ("the marker side", null, 3),
        ];
        var raw = new List<object>();
        output.WriteLine("| Threshold window max | Downsample | Markers matched | Sheets with every marker | Detection time, mean (ms) | Residual RMS, mean (in) | Worst bull, any sheet (in) | Sheets inside the paper gate |");
        output.WriteLine("|---|---|---|---|---|---|---|---|");
        var loaded = SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan && s.Gated)
            .Select(s => (Sample: s, Loaded: ImageLoader.Load(Path.Combine(scans, s.File)))).ToList();
        foreach (var (window, max, downsample) in variants)
        {
            var options = new MeasureOptions(ThresholdWindowMaxPixels: max, DownsampleFactor: downsample);
            Row(window, downsample.ToString(Inv) + "x", loaded.Select(l => Measure(l.Loaded.Image, l.Loaded.Metadata, Phase0Spike.Definition(targets, l.Sample.Definition), l.Sample, options, BullLocatorKind.EdgeFit)).ToList());
        }

        var native = SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan && s.Dpi == 300)
            .Select(s => Measure(scans, targets, s, new MeasureOptions(), BullLocatorKind.EdgeFit)).ToList();
        Row("the marker side, native 300 DPI scans", "none", native);
        RawMeasurements.Write(scans, "threshold", raw);
        return 0;

        void Row(string window, string downsample, List<Run> runs)
        {
            var stats = runs.Select(r => Phase0Spike.Stats(r.Bulls)).ToList();
            output.WriteLine(string.Create(Inv,
                $"| {window} | {downsample} | {runs.Sum(r => r.Fiducials.Matches.Count)} of {runs.Sum(r => r.Fiducials.Expected)} | {runs.Count(r => r.Fiducials.Matches.Count == r.Fiducials.Expected)} of {runs.Count} | {runs.Average(r => r.DetectMs):0} | {In(runs.Average(r => r.Fit?.RmsResidual ?? double.NaN))} | {In(stats.Max(s => s.Worst))} | {stats.Count(s => s.Missing == 0 && s.Worst < Phase0Spike.PaperGate)} of {runs.Count} |"));
            raw.AddRange(runs.Select(r => (object)new
            {
                thresholdWindow = window,
                downsample,
                file = r.Sample.File,
                markersExpected = r.Fiducials.Expected,
                markersMatched = r.Fiducials.Matches.Select(m => m.Id).Order().ToArray(),
                detectMs = r.DetectMs,
                residualRms = RawMeasurements.R(r.Fit?.RmsResidual ?? double.NaN),
                bulls = RawMeasurements.Bulls(r.Bulls, r.Definition),
            }));
        }
    }

    /// <summary>Measurement 5: the print scale of the 96.2 percent sheet against sheet 1, whose ratio is 0.962 by construction.</summary>
    public static int Scale(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var raw = new List<object>();
        output.WriteLine("| DPI | Sheet 1 scale x / y / area | 96.2 percent sheet x / y / area | Ratio x / y / area | Within 0.001 of 0.962 |");
        output.WriteLine("|---|---|---|---|---|");
        foreach (int dpi in (int[])[600, 300])
        {
            var (one, oneRun) = ScaleOf(scans, targets, $"gl-cf25-ltr-1-{dpi}-dpi.png");
            var (scaled, scaledRun) = ScaleOf(scans, targets, $"gl-cf25-ltr-96.2-{dpi}-dpi.png");
            double rx = scaled.ScaleX!.Value / one.ScaleX!.Value, ry = scaled.ScaleY!.Value / one.ScaleY!.Value, ra = scaled.Scale!.Value / one.Scale!.Value;
            bool within = new[] { rx, ry, ra }.All(r => Math.Abs(r - 0.962) <= 0.001);
            output.WriteLine(string.Create(Inv,
                $"| {dpi} | {one.ScaleX:0.00000} / {one.ScaleY:0.00000} / {one.Scale:0.00000} | {scaled.ScaleX:0.00000} / {scaled.ScaleY:0.00000} / {scaled.Scale:0.00000} | {rx:0.00000} / {ry:0.00000} / {ra:0.00000} | {(within ? "yes" : "NO")} |"));
            foreach (var (report, run) in new[] { (one, oneRun), (scaled, scaledRun) })
            {
                raw.Add(new
                {
                    dpi,
                    file = run.Sample.File,
                    report.NominalDpi,
                    report.PixelsPerDmmX,
                    report.PixelsPerDmmY,
                    report.PixelsPerDmmArea,
                    report.ScaleX,
                    report.ScaleY,
                    report.Scale,
                    mapping = RawMeasurements.Mapping(run.Fit!.Mapping),
                });
            }

            raw.Add(new { dpi, ratioX = rx, ratioY = ry, ratioArea = ra });
        }

        RawMeasurements.Write(scans, "scale", raw);
        return 0;
    }

    /// <summary>
    /// Measurement 6: the bull displacement field of the three reference sheets split into its systematic part, the mean
    /// over sheets, and its random part, with an unbiased per-bull RMS. The systematic part is also split into what a
    /// quadratic over the page absorbs, the smooth share a calibration could remove, and what is left. Correlations are
    /// Pearson over the concatenated x and y components, as in PHASE0-PRELIM.md; the rotated rescan is compared with
    /// sheet 2 in page coordinates, and with the scanner-fixed prediction, sheet 2's field negated and reflected through
    /// the centre of the scoring grid.
    /// </summary>
    public static int Field(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        string[] files = ["gl-cf25-ltr-1-600-dpi.png", "gl-cf25-ltr-2-600-dpi.png", "gl-cf25-ltr-3-600-dpi.png", "gl-cf25-ltr-1-300-dpi.png", "gl-cf25-ltr-2-600-dpi-rot180.png"];
        var loaded = files.Select(f => (Sample: SampleSet.All.Single(s => s.File == f), Loaded: ImageLoader.Load(Path.Combine(scans, f)))).ToList();
        var definition = Phase0Spike.Definition(targets, SampleSet.CentreFire);
        var raw = new List<object>();

        output.WriteLine("| Locator | Single sheet mean / worst (in) | Systematic mean / worst (in) | Random RMS (in) | Smooth share of systematic, RMS (in) | Systematic after the smooth share, mean / worst (in) |");
        output.WriteLine("|---|---|---|---|---|---|");
        var correlations = new List<string>();
        foreach (var locator in (BullLocatorKind[])[BullLocatorKind.EdgeFit, BullLocatorKind.Centroid])
        {
            var runs = loaded.Select(l => Measure(l.Loaded.Image, l.Loaded.Metadata, definition, l.Sample, new MeasureOptions(), locator)).ToList();
            var fields = runs.Select(r => r.Bulls.Where(b => b.Recovered is not null).ToDictionary(b => b.Name, b => b, StringComparer.Ordinal)).ToList();
            var labels = fields.Skip(1).Aggregate((IEnumerable<string>)fields[0].Keys, (common, f) => common.Intersect(f.Keys, StringComparer.Ordinal))
                .OrderBy(l => l, StringComparer.Ordinal).ToList();
            var three = fields.Take(3).ToList();
            var position = labels.ToDictionary(l => l, l => three[0][l].Declared, StringComparer.Ordinal);
            var systematic = labels.ToDictionary(l => l, l => new PointD(three.Average(f => f[l].Dx), three.Average(f => f[l].Dy)), StringComparer.Ordinal);
            double single = three.Average(f => labels.Average(l => f[l].Error)), singleWorst = three.Max(f => labels.Max(l => f[l].Error));
            double randomSum = three.Sum(f => labels.Sum(l => Math.Pow(f[l].Dx - systematic[l].X, 2) + Math.Pow(f[l].Dy - systematic[l].Y, 2)));
            double random = Math.Sqrt(randomSum / (labels.Count * (three.Count - 1)));
            var smooth = Quadratic(labels.Select(l => position[l]).ToList(), labels.Select(l => systematic[l]).ToList());
            double smoothRms = Math.Sqrt(smooth.Average(s => (s.X * s.X) + (s.Y * s.Y)));
            var after = labels.Select((l, i) => Math.Sqrt(Math.Pow(systematic[l].X - smooth[i].X, 2) + Math.Pow(systematic[l].Y - smooth[i].Y, 2))).ToList();
            var magnitude = labels.Select(l => Math.Sqrt((systematic[l].X * systematic[l].X) + (systematic[l].Y * systematic[l].Y))).ToList();
            output.WriteLine(string.Create(Inv,
                $"| {locator} | {In(single)} / {In(singleWorst)} | {In(magnitude.Average())} / {In(magnitude.Max())} | {In(random)} | {In(smoothRms)} | {In(after.Average())} / {In(after.Max())} |"));

            double Correlate(Func<string, (double X, double Y)> a, Func<string, (double X, double Y)> b, IReadOnlyList<string> over)
            {
                var x = over.SelectMany(l => new[] { a(l).X, a(l).Y }).ToArray();
                var y = over.SelectMany(l => new[] { b(l).X, b(l).Y }).ToArray();
                double mx = x.Average(), my = y.Average();
                double sxy = x.Zip(y, (p, q) => (p - mx) * (q - my)).Sum();
                return sxy / Math.Sqrt(x.Sum(p => (p - mx) * (p - mx)) * y.Sum(q => (q - my) * (q - my)));
            }

            (double X, double Y) D(int sheet, string label) => (fields[sheet][label].Dx, fields[sheet][label].Dy);
            var scoring = labels.Where(l => definition.Bulls[three[0][l].Index].Scoring).ToList();
            double gx = scoring.Average(l => position[l].X), gy = scoring.Average(l => position[l].Y);
            var reflected = scoring.ToDictionary(l => l, l => scoring.FirstOrDefault(o =>
                Math.Abs(position[o].X - ((2 * gx) - position[l].X)) < 5 && Math.Abs(position[o].Y - ((2 * gy) - position[l].Y)) < 5), StringComparer.Ordinal);
            var paired = scoring.Where(l => reflected[l] is not null).ToList();
            double[] r = [
                Correlate(l => D(0, l), l => D(1, l), labels), Correlate(l => D(0, l), l => D(2, l), labels), Correlate(l => D(1, l), l => D(2, l), labels),
                Correlate(l => D(0, l), l => D(3, l), labels), Correlate(l => D(4, l), l => D(1, l), labels),
                Correlate(l => D(4, l), l => (-D(1, reflected[l]!).X, -D(1, reflected[l]!).Y), paired),
            ];
            correlations.Add(string.Create(Inv, $"| {locator} | {r[0]:+0.000;-0.000} | {r[1]:+0.000;-0.000} | {r[2]:+0.000;-0.000} | {r[3]:+0.000;-0.000} | {r[4]:+0.000;-0.000} | {r[5]:+0.000;-0.000} |"));

            raw.Add(new
            {
                locator,
                images = runs.Select(run => new { file = run.Sample.File, mapping = RawMeasurements.Mapping(run.Fit?.Mapping), bulls = RawMeasurements.Bulls(run.Bulls, definition) }).ToArray(),
                systematic = labels.Select((l, i) => new
                {
                    label = l,
                    declaredX = position[l].X,
                    declaredY = position[l].Y,
                    dx = RawMeasurements.R(systematic[l].X),
                    dy = RawMeasurements.R(systematic[l].Y),
                    smoothDx = RawMeasurements.R(smooth[i].X),
                    smoothDy = RawMeasurements.R(smooth[i].Y),
                }).ToArray(),
                scannerFixedPairs = paired.Select(l => new { label = l, reflectedFrom = reflected[l] }).ToArray(),
                correlations = new { sheet1Sheet2 = r[0], sheet1Sheet3 = r[1], sheet2Sheet3 = r[2], sheet1At600Against300 = r[3], rotatedAgainstSheet2 = r[4], rotatedAgainstScannerFixedPrediction = r[5] },
            });
        }

        output.WriteLine();
        output.WriteLine("| Locator | Sheet 1 against 2 | 1 against 3 | 2 against 3 | Sheet 1, 600 against 300 DPI | Rotated rescan against sheet 2 | Rotated rescan against the scanner-fixed prediction |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        foreach (string line in correlations)
        {
            output.WriteLine(line);
        }

        RawMeasurements.Write(scans, "field", raw);
        return 0;
    }

    private static Run Measure(string scans, string targets, SampleSet.Sample sample, MeasureOptions options, BullLocatorKind? locator)
    {
        var (image, metadata) = ImageLoader.Load(Path.Combine(scans, sample.File));
        return Measure(image, metadata, Phase0Spike.Definition(targets, sample.Definition), sample, options, locator);
    }

    private static Run Measure(GrayImage image, ImageMetadata metadata, TargetDefinition definition, SampleSet.Sample sample, MeasureOptions options, BullLocatorKind? locator)
    {
        var trace = new TraceRecorder();
        var clock = Stopwatch.StartNew();
        var fiducials = SheetMeasurer.DetectFiducials(image, metadata, definition, options with { TileIndex = sample.Tile }, Backend, trace);
        long ms = clock.ElapsedMilliseconds;
        var fit = fiducials.Matches.Count >= 4 ? SheetMeasurer.Register(image, metadata, fiducials, options, Backend, trace) : null;
        IReadOnlyList<BullLocation> bulls = fit is not null && locator is { } kind
            ? SheetMeasurer.LocateBulls(image, definition, fit.Mapping, options with { Locator = kind }, trace)
            : [];
        return new Run(sample, image, metadata, definition, fiducials, fit, bulls, ms);
    }

    private static (ScaleReport Scale, Run Run) ScaleOf(string scans, string targets, string file)
    {
        var sample = SampleSet.All.Single(s => s.File == file);
        var run = Measure(scans, targets, sample, new MeasureOptions(), null);
        return (SheetMeasurer.VerifyScale(run.Metadata, run.Definition, new MeasureOptions(), run.Fit!, run.Image, new TraceRecorder()).Scale, run);
    }

    private static bool IsReferenceSheet(SampleSet.Sample s) =>
        s.Kind == SampleSet.SampleKind.Scan && s.Definition == SampleSet.CentreFire && s.Description.StartsWith("sheet ", StringComparison.Ordinal)
        && !s.File.Contains("rot180", StringComparison.Ordinal);

    /// <summary>The least-squares quadratic over page position fitted to a vector field, evaluated at the same points.</summary>
    private static List<PointD> Quadratic(List<PointD> at, List<PointD> field)
    {
        static double[] Terms(PointD p)
        {
            double x = (p.X - 1080) / 1000, y = (p.Y - 1397) / 1000;
            return [1, x, y, x * x, x * y, y * y];
        }

        var normal = new double[6, 6];
        var bx = new double[6];
        var by = new double[6];
        for (int i = 0; i < at.Count; i++)
        {
            var t = Terms(at[i]);
            for (int a = 0; a < 6; a++)
            {
                bx[a] += t[a] * field[i].X;
                by[a] += t[a] * field[i].Y;
                for (int b = 0; b < 6; b++)
                {
                    normal[a, b] += t[a] * t[b];
                }
            }
        }

        var cx = LinearSolve.Solve(normal, bx) ?? new double[6];
        var cy = LinearSolve.Solve(normal, by) ?? new double[6];
        return [.. at.Select(p =>
        {
            var t = Terms(p);
            return new PointD(t.Zip(cx, (u, c) => u * c).Sum(), t.Zip(cy, (u, c) => u * c).Sum());
        })];
    }

    private static double Squared(PointD a, PointD b) => Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2);

    private static double Percentile(List<double> values, double q)
    {
        if (values.Count == 0)
        {
            return double.NaN;
        }

        var sorted = values.Order().ToList();
        return sorted[Math.Min(sorted.Count - 1, (int)Math.Floor((q * (sorted.Count - 1)) + 0.5))];
    }

    private static string In(double dmm) => double.IsNaN(dmm) ? "" : (dmm / 254).ToString("0.00000", Inv);

    private static string Verdict((double Mean, double Worst, int Missing) s) => s.Missing == 0 && s.Worst < Phase0Spike.PaperGate ? "pass" : "fail";
}
