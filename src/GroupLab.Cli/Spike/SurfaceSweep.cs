using System.Globalization;
using System.Text.Json;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Cli.Spike;

/// <summary>
/// PHASE1-BRIEF.md section 3.3: the developable surface fit against synthetic truth, before any real frame. The truth camera
/// is the Phase 0 main camera as the flat frames measured it: a 3000 by 4000 frame, a lens of k1 -0.05 and k2 +0.065, the
/// sheet filling the frame at about a pixel per dmm as <c>main_flat1</c> does, and a focal length of 2600 px against the
/// 2556 px the EXIF estimate starts from. Each axis moves one thing away from a baseline bow of a quarter inch, square on
/// but for an 8 degree tilt, until the gate breaks.
/// </summary>
public static class SurfaceSweep
{
    private const int Width = 3000, Height = 4000, Seeds = 10;
    private const double TruthFocal = 2600, K1 = -0.05, K2 = 0.065, Distance = 2600, Gate = 1.27;
    private static readonly double ExifFocal = 4000 * 23 / 36.0;
    private static readonly double BaselineTilt = 8 * Math.PI / 180;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private sealed record Trial(string Axis, string Level, double Value, int Seed, double Noise, int Markers, double Keystone, double TwistIn,
        double TruthDeflectionIn, double FittedDeflectionIn, double TruthRulingDegrees, double FittedRulingDegrees, double StartFocal, double FittedFocal,
        int Kept, int Corners, double RmsKeptIn, double SurfaceWorstIn, double SurfaceScoringIn, double SurfaceSighterIn, double PlanarWorstIn, double PlanarScoringIn,
        double F, bool PreferSurface, double SelectedWorstIn, double SelectedScoringIn, double SelectedSighterIn, IReadOnlyList<object> Bulls);

    public static int Synthetic(string frozenDirectory, string phase0Measurements, string phase1Scans, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var definition = Phase0Spike.Definition(frozenDirectory, SampleSet.CentreFire);
        double w = definition.Page.Width, h = definition.Page.Height;
        int[] flat3 = MarkersDecoded(phase0Measurements, "main_flat3.jpg");
        var trials = new List<Trial>();

        SurfaceModel Bow(double inches, double tilt) => Solve(inches, c => Truth(Math.PI / 2, [0, c, 0, 0], tilt), w, h);
        SurfaceModel Curl(double inches) => Solve(inches, c =>
        {
            // Tangent angle rising as the cube of distance down the page from the top edge: flat at the top, curled forward at
            // the free bottom edge. The constant term is a rigid turn about the rulings, which the camera pose absorbs.
            double half = h / 2 / SurfaceModel.BendLength;
            return Truth(0, [-c / 8, -3 * c / (8 * half), -3 * c / (8 * half * half), -c / (8 * half * half * half)], BaselineTilt);
        }, w, h);

        void Cell(string axis, string level, double value, SurfaceModel truth, double twist, Func<int, IReadOnlyCollection<int>?> subset, double startFocal, bool noiseFree)
        {
            foreach (var (noise, seeds) in noiseFree ? new[] { (0.0, 1), (SyntheticSurface.MeasuredCornerNoisePixels, Seeds) } : [(SyntheticSurface.MeasuredCornerNoisePixels, Seeds)])
            {
                for (int seed = 1; seed <= seeds; seed++)
                {
                    if (Run(definition, axis, level, value, truth, twist, subset(seed), noise, seed, startFocal) is { } t)
                    {
                        trials.Add(t);
                    }
                }
            }
        }

        foreach (double d in (double[])[0, 0.05, 0.1, 0.25, 0.5, 1.0, 1.5, 2.0])
        {
            Cell("bow", string.Create(Inv, $"{d:0.00} in"), d, Bow(d, BaselineTilt), 0, _ => null, ExifFocal, noiseFree: true);
        }

        foreach (double d in (double[])[0.05, 0.1, 0.25, 0.5, 1.0, 1.5, 2.0])
        {
            Cell("curl", string.Create(Inv, $"{d:0.00} in"), d, Curl(d), 0, _ => null, ExifFocal, noiseFree: true);
        }

        var baseline = Bow(0.25, BaselineTilt);
        foreach (double d in (double[])[0, 0.02, 0.05, 0.1, 0.25, 0.5])
        {
            Cell("twist", string.Create(Inv, $"{d:0.00} in over a 0.25 in bow"), d, baseline, d * 254, _ => null, ExifFocal, noiseFree: false);
        }

        var ids = PageRegistration.ExpectedMarkers(definition, 0).Select(m => m.Id).ToArray();
        Cell("markers", "all 34", 34, baseline, 0, _ => null, ExifFocal, noiseFree: false);
        Cell("markers", "the 23 main_flat3 decoded", 23, baseline, 0, _ => flat3, ExifFocal, noiseFree: false);
        // The first markers in raster order, the same set on every seed: the top of the sheet. Until PHASE1-RESULTS.md M1.7
        // these rows read "at random", from a shuffle that drew a fresh generator with the same seed for every marker, so every
        // key was equal and the order never changed.
        foreach (int count in (int[])[23, 16, 12, 9])
        {
            Cell("markers", $"the first {count} in raster order", count, baseline, 0, _ => ids.Take(count).ToArray(), ExifFocal, noiseFree: false);
        }

        foreach (double k in (double[])[1.0, 0.95, 0.9, 0.85, 0.802, 0.75, 0.7])
        {
            double tilt = TiltFor(k, w, h);
            Cell("keystone", string.Create(Inv, $"{k:0.000}"), k, Bow(0.25, tilt), 0, _ => null, ExifFocal, noiseFree: false);
        }

        foreach (double ratio in (double[])[0.55, 0.75, 1.0, 1.3, 1.8])
        {
            Cell("start focal", string.Create(Inv, $"{ratio:0.00} of truth"), ratio, baseline, 0, _ => null, TruthFocal * ratio, noiseFree: false);
        }

        output.WriteLine($"Truth camera: {Width} by {Height}, focal {TruthFocal} px, k1 {K1}, k2 {K2}, {Distance / 10} mm from the page centre, tilted {BaselineTilt * 180 / Math.PI:0} degrees unless the keystone axis says otherwise. " +
            string.Create(Inv, $"Corner noise {SyntheticSurface.MeasuredCornerNoisePixels} px per axis, {Seeds} seeds per cell, plus one noise-free trial on the bend axes. Inches."));
        output.WriteLine();
        output.WriteLine("| Axis | Level | Noise (px) | Trials | Markers | Truth / fitted deflection, median | Fitted focal, median (px) | Corners kept, median | Surface worst bull, median / 90th pct | Scoring / sighter worst, median | Whole-sheet lens model worst, median | Surface gate | Bend chosen | Selected worst bull, median / 90th pct | Selected gate |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var cell in trials.GroupBy(t => (t.Axis, t.Level, t.Noise)))
        {
            var c = cell.ToList();
            double median = Percentile(c.Select(t => t.SurfaceWorstIn), 0.5), p90 = Percentile(c.Select(t => t.SurfaceWorstIn), 0.9);
            output.WriteLine(string.Create(Inv,
                $"| {cell.Key.Axis} | {cell.Key.Level} | {cell.Key.Noise:0.00} | {c.Count} | {Percentile(c.Select(t => (double)t.Markers), 0.5):0} | {c[0].TruthDeflectionIn:0.000} / {Percentile(c.Select(t => t.FittedDeflectionIn), 0.5):0.000} | {Percentile(c.Select(t => t.FittedFocal), 0.5):0} | {Percentile(c.Select(t => (double)t.Kept), 0.5):0} of {c[0].Corners} | {median:0.00000} / {p90:0.00000} | {Percentile(c.Select(t => t.SurfaceScoringIn), 0.5):0.00000} / {Percentile(c.Select(t => t.SurfaceSighterIn), 0.5):0.00000} | {Percentile(c.Select(t => t.PlanarWorstIn), 0.5):0.00000} | {Verdict(median, p90)} | {c.Count(t => t.PreferSurface)} of {c.Count} | {Percentile(c.Select(t => t.SelectedWorstIn), 0.5):0.00000} / {Percentile(c.Select(t => t.SelectedWorstIn), 0.9):0.00000} | {Verdict(Percentile(c.Select(t => t.SelectedWorstIn), 0.5), Percentile(c.Select(t => t.SelectedWorstIn), 0.9))} |"));
        }

        RawMeasurements.Write(phase1Scans, "surface-synthetic", trials);
        return 0;
    }

    /// <summary>Rendered bent sheets through the whole pipeline: detection, registration by both models, and the edge-fit locator.</summary>
    public static int Rendered(string frozenDirectory, string phase1Scans, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var definition = Phase0Spike.Definition(frozenDirectory, SampleSet.CentreFire);
        double w = definition.Page.Width, h = definition.Page.Height;
        // The frozen as-printed sheet raises test 26f on its sighters, an error since NOTES-FROM-PLANNING.md entry 13; it is
        // a measurement input, which is rendered as printed rather than refused (entry 11 item 4).
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition, new RenderOptions(AllowInvalid: true)).Pages[0], 300);
        var backend = new OpenCvSharpBackend();
        var metadata = new ImageMetadata("synthetic", Width, Height, null, null, "synthetic", "main camera", 1, 6.25, 23, 1.7);
        (string Name, SurfaceModel Truth, double Twist)[] cases =
        [
            ("flat", Solve(0, c => Truth(Math.PI / 2, [0, c, 0, 0], BaselineTilt), w, h), 0),
            ("bow 0.25 in", Solve(0.25, c => Truth(Math.PI / 2, [0, c, 0, 0], BaselineTilt), w, h), 0),
            ("bow 1.00 in", Solve(1.0, c => Truth(Math.PI / 2, [0, c, 0, 0], BaselineTilt), w, h), 0),
            ("bow 0.25 in at keystone 0.802", Solve(0.25, c => Truth(Math.PI / 2, [0, c, 0, 0], TiltFor(0.802, w, h)), w, h), 0),
            ("bow 0.25 in with a 0.10 in twist", Solve(0.25, c => Truth(Math.PI / 2, [0, c, 0, 0], BaselineTilt), w, h), 25.4),
        ];

        var rows = new List<object>();
        output.WriteLine("| Case | Truth deflection (in) | Model | Markers | Corners kept | Fitted deflection (in) | Fitted focal (px) | Bull mean (in) | Worst scoring bull (in) | Worst sighter (in) | Gate |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var (name, truth, twist) in cases)
        {
            var photograph = WarpRasterizer.Render(render, 300, p => SyntheticSurface.Image(truth, twist, w, h, p), Width, Height, w, h);
            foreach (var model in (RegistrationModel[])[RegistrationModel.Radial, RegistrationModel.Surface])
            {
                var m = SheetMeasurer.Measure(photograph, metadata, definition, new MeasureOptions(Model: model), backend);
                var located = m.Bulls.Where(b => b.Recovered is not null).ToList();
                var scoring = located.Where(b => definition.Bulls[b.Index].Scoring).DefaultIfEmpty().MaxBy(b => b?.Error ?? 0);
                var sighter = located.Where(b => !definition.Bulls[b.Index].Scoring).DefaultIfEmpty().MaxBy(b => b?.Error ?? 0);
                var surface = m.Registration?.Mapping as SurfaceMapping;
                double fittedDeflection = surface is null ? double.NaN : DevelopableSurface.Deflection(surface.Parameters, w, h) / 254;
                bool pass = m.Failure is null && located.Count == definition.Bulls.Count && m.WorstError < Gate;
                output.WriteLine(string.Create(Inv,
                    $"| {name} | {DevelopableSurface.Deflection(truth, w, h) / 254:0.000} | {(model == RegistrationModel.Surface ? "surface" : "whole-sheet lens")} | {m.Fiducials?.Matches.Count}/{m.Fiducials?.Expected} | {m.Registration?.Inliers} of {m.Registration?.Corners.Count} | {(surface is null ? "" : fittedDeflection.ToString("0.000", Inv))} | {(surface is null ? "" : surface.Parameters.FocalPixels.ToString("0", Inv))} | {m.MeanError / 254:0.00000} | {(scoring?.Error ?? double.NaN) / 254:0.00000} at {scoring?.Name} | {(sighter?.Error ?? double.NaN) / 254:0.00000} at {sighter?.Name} | {(pass ? "pass" : "fail")} |"));
                rows.Add(new
                {
                    name,
                    model,
                    truth,
                    twistDmm = twist,
                    truthDeflectionIn = DevelopableSurface.Deflection(truth, w, h) / 254,
                    failure = m.Failure,
                    markers = m.Fiducials?.Matches.Select(x => x.Id).Order().ToArray(),
                    mapping = RawMeasurements.Mapping(m.Registration?.Mapping),
                    corners = m.Registration is { } f ? RawMeasurements.Corners(f) : null,
                    bulls = RawMeasurements.Bulls(m.Bulls, definition),
                });
            }
        }

        RawMeasurements.Write(phase1Scans, "surface-rendered", rows);
        return 0;
    }

    private static Trial? Run(TargetDefinition definition, string axis, string level, double value, SurfaceModel truth, double twist,
        IReadOnlyCollection<int>? subset, double noise, int seed, double startFocal)
    {
        double w = definition.Page.Width, h = definition.Page.Height;
        var matches = SyntheticSurface.Corners(definition, truth, twist, noise, new Random((seed * 7919) + (int)(value * 1000)), Width, Height, subset);
        if (matches.Count < 4)
        {
            return null;
        }

        var image = matches.SelectMany(m => m.ImageCorners).ToList();
        var page = matches.SelectMany(m => m.PageCorners).ToList();
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var start = SurfaceFit.StartFromLens(lens, startFocal, w / 2, h / 2);
        var fit = SurfaceFit.Fit([new SurfaceFrame(axis, image, page, [.. image.Select(_ => true)], start, 0, 0, w, h)], shareCamera: false)[0];

        var choice = SurfaceSelection.Choose(fit, image, page, Width, Height);
        IPageMapping selected = choice.PreferSurface ? fit.Mapping : choice.Planar!;
        var bulls = definition.Bulls.Select((b, i) =>
        {
            var declared = new PointD(b.X, b.Y);
            var at = SyntheticSurface.Image(truth, twist, w, h, declared);
            return (b.Label, b.Scoring, Surface: Dist(fit.Mapping.ToPage(at), declared), Planar: Dist(lens.ToPage(at), declared), Selected: Dist(selected.ToPage(at), declared));
        }).ToList();

        return new Trial(axis, level, value, seed, noise, matches.Count, SyntheticSurface.Keystone(truth, w, h), twist / 254,
            DevelopableSurface.Deflection(truth, w, h) / 254, fit.DeflectionDmm / 254, Degrees(truth.RulingAngle), Degrees(fit.Model.RulingAngle),
            startFocal, fit.Model.FocalPixels, fit.Kept.Count(k => k), fit.Kept.Count, fit.RmsKept / 254,
            bulls.Max(b => b.Surface) / 254, bulls.Where(b => b.Scoring).Max(b => b.Surface) / 254, bulls.Where(b => !b.Scoring).Max(b => b.Surface) / 254,
            bulls.Max(b => b.Planar) / 254, bulls.Where(b => b.Scoring).Max(b => b.Planar) / 254,
            choice.F, choice.PreferSurface, bulls.Max(b => b.Selected) / 254, bulls.Where(b => b.Scoring).Max(b => b.Selected) / 254, bulls.Where(b => !b.Scoring).Max(b => b.Selected) / 254,
            [.. bulls.Select(b => (object)new { label = b.Label, b.Scoring, surfaceDmm = RawMeasurements.R(b.Surface), planarDmm = RawMeasurements.R(b.Planar), selectedDmm = RawMeasurements.R(b.Selected) })]);
    }

    private static SurfaceModel Truth(double rulingAngle, double[] bend, double tilt) =>
        SyntheticSurface.Camera(Width, Height, TruthFocal, K1, K2, Distance, tilt, 2159, 2794, rulingAngle, bend);

    /// <summary>The model from a one-parameter family whose deflection is <paramref name="inches"/>, by bisection on the parameter.</summary>
    private static SurfaceModel Solve(double inches, Func<double, SurfaceModel> family, double w, double h)
    {
        double target = inches * 254, lo = 0, hi = 0.01;
        if (target <= 0)
        {
            return family(0);
        }

        while (DevelopableSurface.Deflection(family(hi), w, h) < target)
        {
            hi *= 2;
        }

        for (int i = 0; i < 80; i++)
        {
            double mid = (lo + hi) / 2;
            (lo, hi) = DevelopableSurface.Deflection(family(mid), w, h) < target ? (mid, hi) : (lo, mid);
        }

        return family((lo + hi) / 2);
    }

    /// <summary>The tilt, radians, that gives the flat sheet a top-to-bottom keystone of <paramref name="keystone"/>.</summary>
    private static double TiltFor(double keystone, double w, double h)
    {
        if (keystone >= 1)
        {
            return 0;
        }

        double sign = SyntheticSurface.Keystone(Truth(0, [0, 0, 0, 0], 0.1), w, h) < 1 ? 1 : -1;
        double lo = 0, hi = 1.0;
        for (int i = 0; i < 80; i++)
        {
            double mid = (lo + hi) / 2;
            (lo, hi) = SyntheticSurface.Keystone(Truth(0, [0, 0, 0, 0], sign * mid), w, h) > keystone ? (mid, hi) : (lo, mid);
        }

        return sign * (lo + hi) / 2;
    }

    internal static int[] MarkersDecoded(string measurements, string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(measurements, "photos.json")));
        return [.. document.RootElement.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("file").GetString() == file)
            .GetProperty("markersMatched").EnumerateArray().Select(v => v.GetInt32())];
    }

    private static double Percentile(IEnumerable<double> values, double q)
    {
        var sorted = values.Where(v => !double.IsNaN(v)).Order().ToList();
        return sorted.Count == 0 ? double.NaN : sorted[Math.Min(sorted.Count - 1, (int)Math.Floor((q * (sorted.Count - 1)) + 0.5))];
    }

    private static string Verdict(double median, double p90) => p90 < Gate / 254 ? "pass" : median < Gate / 254 ? "median passes, 90th fails" : "fail";

    private static double Degrees(double radians) => ((radians * 180 / Math.PI % 180) + 180) % 180;

    private static double Dist(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
