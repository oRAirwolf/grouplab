using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Cli.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 16 section 5: the general developable surface (<see cref="FoldedSheet"/>) on the same
/// discipline as the cylinder. <see cref="Sweep"/> is synthetic truth, swept until it breaks, fitting the cylinder and the
/// general surface to the same corners of M1.7's truth camera: rulings that turn linearly (a cone) and quadratically across a
/// 0.40 in bow, a twist no developable surface takes, corner noise, the marker sets the Phase 0 frames decoded, and a larger
/// bow. <see cref="Frames"/> then runs once on the seven mounted frames with the three flat frames as control, grouped as
/// PHASE1-RESULTS.md M1.9 groups them.
/// </summary>
public static class SurfaceGeneral
{
    private const int Width = 3000, Height = 4000, Seeds = 10;
    private const double Gate = 1.27;
    private static readonly double ExifFocal = 4000 * 23 / 36.0;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly object ProgressLock = new();

    private sealed record Cell(string Axis, string Level, SurfaceModel Truth, double TwistDmm, Func<int, IReadOnlyCollection<int>?> Subset, double Noise);

    private sealed record Trial(string Axis, string Level, int Seed, double Noise, int Markers, double TruthDeflectionIn, double TruthTurnDegrees,
        double CylinderWorstIn, double CylinderScoringIn, double CylinderSighterIn, int CylinderKept, double CylinderRobustPx,
        double GeneralWorstIn, double GeneralScoringIn, double GeneralSighterIn, int GeneralKept, int Corners, double GeneralRobustPx,
        double GeneralDeflectionIn, double GeneralTurnDegrees, IReadOnlyList<double> GeneralTurn, double GeneralF, double GeneralCriticalF, bool GeneralPreferSurface,
        double SelectedWorstIn);

    public static int Sweep(string frozenDirectory, string phase0Measurements, string phase1Scans, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var clock = Stopwatch.StartNew();
        void Progress(string line)
        {
            lock (ProgressLock)
            {
                output.WriteLine(string.Create(Inv, $"[{clock.Elapsed.TotalSeconds,6:0}s] {line}"));
                output.Flush();
            }
        }

        var definition = Phase0Spike.Definition(frozenDirectory, SampleSet.CentreFire);
        double w = definition.Page.Width, h = definition.Page.Height;
        var ids = PageRegistration.ExpectedMarkers(definition, 0).Select(m => m.Id).ToArray();
        var bow = SurfaceLens.Bow(0.40, w, h);
        SurfaceModel Turned(SurfaceModel m, double linear, double quadratic) => m with { Family = SurfaceFamily.General, Turn = [linear, quadratic] };
        string Turn(SurfaceModel m) => string.Create(Inv, $"rulings turn {TurnDegrees(m, w, h):0.0} degrees across the page");

        var cells = new List<Cell>();
        foreach (double linear in (double[])[0, 0.1, 0.2, 0.3, 0.4])
        {
            var truth = Turned(bow, linear, 0);
            cells.Add(new Cell("cone, linear turn", string.Create(Inv, $"{linear:0.0}: {Turn(truth)}"), truth, 0, _ => null, SyntheticSurface.MeasuredCornerNoisePixels));
        }

        foreach (double quadratic in (double[])[0.05, 0.1, 0.2])
        {
            var truth = Turned(bow, 0, quadratic);
            cells.Add(new Cell("quadratic turn", string.Create(Inv, $"{quadratic:0.00}: {Turn(truth)}"), truth, 0, _ => null, SyntheticSurface.MeasuredCornerNoisePixels));
        }

        foreach (double twist in (double[])[0.1, 0.25, 0.5])
        {
            cells.Add(new Cell("twist, not developable", string.Create(Inv, $"{twist:0.00} in over the bow"), bow, twist * 254, _ => null, SyntheticSurface.MeasuredCornerNoisePixels));
        }

        var cone = Turned(bow, 0.2, 0);
        foreach (double noise in (double[])[1.0, 1.5, 2.0])
        {
            cells.Add(new Cell("noise, cone 0.2", string.Create(Inv, $"{noise:0.0} px"), cone, 0, _ => null, noise));
        }

        foreach (string file in (string[])["main2.jpg", "main3.jpg"])
        {
            var decoded = SurfaceSweep.MarkersDecoded(phase0Measurements, file);
            cells.Add(new Cell("markers, cone 0.2", $"{Path.GetFileNameWithoutExtension(file)}'s {decoded.Length}", cone, 0, _ => decoded, SyntheticSurface.MeasuredCornerNoisePixels));
        }

        cells.Add(new Cell("markers, cone 0.2", "25 at random", cone, 0, seed =>
        {
            var random = new Random(25000 + seed);
            return ids.OrderBy(_ => random.Next()).Take(25).ToArray();
        }, SyntheticSurface.MeasuredCornerNoisePixels));
        var bigBow = Turned(SurfaceLens.Bow(1.0, w, h), 0.2, 0);
        cells.Add(new Cell("bow, cone 0.2", string.Create(Inv, $"1.00 in, {Turn(bigBow)}"), bigBow, 0, _ => null, SyntheticSurface.MeasuredCornerNoisePixels));

        foreach (var cell in cells.Where(c => c.Truth.Family == SurfaceFamily.General && !FoldedSheet.For(c.Truth).Valid).ToList())
        {
            Progress($"{cell.Axis}, {cell.Level}: the truth's rulings cross inside the page, so no sheet of paper takes it; not run");
            cells.Remove(cell);
        }

        var specs = cells.SelectMany((c, ci) => Enumerable.Range(1, Seeds).Select(seed => (Cell: c, Index: ci, Seed: seed))).ToList();
        Progress($"{specs.Count} synthetic trials in {cells.Count} cells, each fitted as a cylinder and as a general developable surface");
        var results = new Trial?[specs.Count];
        var done = new ConcurrentDictionary<int, int>();
        Parallel.For(0, specs.Count, i =>
        {
            var (cell, index, seed) = specs[i];
            results[i] = Run(definition, cell, seed, (seed * 7919) + (index * 104729));
            if (done.AddOrUpdate(index, 1, (_, v) => v + 1) == Seeds)
            {
                var trials = specs.Select((s, k) => (s, k)).Where(x => x.s.Index == index).Select(x => results[x.k]).OfType<Trial>().ToList();
                Progress(string.Create(Inv, $"{cell.Axis}, {cell.Level}: worst bull median cylinder {Percentile(trials.Select(t => t.CylinderWorstIn), 0.5):0.00000} in, general {Percentile(trials.Select(t => t.GeneralWorstIn), 0.5):0.00000} in"));
            }
        });

        var all = results.OfType<Trial>().ToList();
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"M1.7's truth camera at a 0.40 in bow unless the level says otherwise, lens free, {Seeds} seeds per cell, corner noise 0.52 px per axis unless the level says otherwise. Worst bull, inches."));
        output.WriteLine();
        output.WriteLine("| Axis | Level | Noise (px) | Markers | Truth deflection (in) | Cylinder: worst bull median / 90th pct | Cylinder gate | General: worst bull median / 90th pct | General gate | General: corners kept, robust sigma (px) | General: fitted turn across the page (deg), median | General bend kept | Selected: worst bull median / 90th pct | Selected gate |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var group in all.GroupBy(t => (t.Axis, t.Level)))
        {
            var t = group.ToList();
            (double Median, double P90) Of(Func<Trial, double> f) => (Percentile(t.Select(f), 0.5), Percentile(t.Select(f), 0.9));
            var c = Of(x => x.CylinderWorstIn);
            var g = Of(x => x.GeneralWorstIn);
            var s = Of(x => x.SelectedWorstIn);
            output.WriteLine(string.Create(Inv,
                $"| {group.Key.Axis} | {group.Key.Level} | {t[0].Noise:0.00} | {t[0].Markers} | {t[0].TruthDeflectionIn:0.000} | {c.Median:0.00000} / {c.P90:0.00000} | {Verdict(c)} | {g.Median:0.00000} / {g.P90:0.00000} | {Verdict(g)} | {Percentile(t.Select(x => (double)x.GeneralKept), 0.5):0} of {t[0].Corners}, {Percentile(t.Select(x => x.GeneralRobustPx), 0.5):0.00} | {Percentile(t.Select(x => x.GeneralTurnDegrees), 0.5):0.0} of {t[0].TruthTurnDegrees:0.0} | {t.Count(x => x.GeneralPreferSurface)} of {t.Count} | {s.Median:0.00000} / {s.P90:0.00000} | {Verdict(s)} |"));
        }

        RawMeasurements.Write(phase1Scans, "surface-general-synthetic", all);
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"done in {clock.Elapsed.TotalMinutes:0.0} minutes"));
        return 0;
    }

    public static int Frames(string scans, string frozenDirectory, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var clock = Stopwatch.StartNew();
        void Progress(string line)
        {
            lock (ProgressLock)
            {
                output.WriteLine(string.Create(Inv, $"[{clock.Elapsed.TotalSeconds,6:0}s] {line}"));
                output.Flush();
            }
        }

        var samples = SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Photograph && s.Excluded is null).ToList();
        Progress($"preparing {samples.Count} photographs");
        var photos = new SurfaceFrames.Prepared[samples.Count];
        Parallel.For(0, samples.Count, i =>
        {
            photos[i] = SurfaceFrames.Prepare(scans, frozenDirectory, samples[i], new OpenCvSharpBackend());
            Progress($"prepared {photos[i].Sample.File}{(photos[i].Failure is null ? "" : ", " + photos[i].Failure)}");
        });

        Progress("the generalised cylinder, as PHASE1-RESULTS.md M1.9 fits it");
        var (cylinder, _) = SurfaceFrames.FitByLens(photos, Progress);
        Progress("the general developable surface");
        var (general, _) = SurfaceFrames.FitByLens(photos, Progress, SurfaceFamily.General);

        var gated = photos.Where(p => p.Frame is not null && p.Sample.Gate != SampleSet.PhotographGate.None)
            .OrderBy(p => p.Sample.Gate).ThenBy(p => p.Sample.File, StringComparer.Ordinal).ToList();
        var lines = new string[gated.Count];
        var shapeLines = new string[gated.Count];
        var aloneLines = new string[gated.Count];
        var rows = new object[gated.Count];
        Parallel.For(0, gated.Count, i =>
        {
            var p = gated[i];
            string file = p.Sample.File, gate = p.Sample.Gate == SampleSet.PhotographGate.Mounted ? "mounted" : "flat";
            var c = SurfaceFrames.Evaluate(p, cylinder[file]);
            var fit = general[file];
            var g = SurfaceFrames.Evaluate(p, fit);
            var whole = SurfaceFrames.FromBulls("whole sheet", p.BaselineBulls, p.Definition);
            var twist = SurfaceTwist.Estimate(p.Frame!, fit.Mapping, [.. fit.PageErrors.Select(e => e <= SurfaceFit.MisreadThreshold)]);
            var cylinderTwist = SurfaceTwist.Estimate(p.Frame!, cylinder[file].Mapping, [.. cylinder[file].PageErrors.Select(e => e <= SurfaceFit.MisreadThreshold)]);
            double robust = Robust(p, fit), cylinderRobust = Robust(p, cylinder[file]);
            double turn = TurnDegrees(fit.Model, p.Definition.Page.Width, p.Definition.Page.Height);

            // Each frame fitted alone, its own focal length and lens, so that no frame's fit can hold up another's.
            var cylinderAlone = SurfaceFit.Fit([SurfaceFrames.AtFocal(p, p.ExifFocal!.Value)], shareCamera: false)[0];
            var generalAlone = SurfaceFit.Fit([SurfaceFrames.AtFocal(p, p.ExifFocal!.Value)], shareCamera: false, SurfaceHold.None, SurfaceFamily.General)[0];
            var ca = SurfaceFrames.Evaluate(p, cylinderAlone);
            var ga = SurfaceFrames.Evaluate(p, generalAlone);
            aloneLines[i] = string.Create(Inv,
                $"| {gate} | `{file}` | {Forward(p, cylinderAlone):0.00} / {Forward(p, generalAlone):0.00} | {cylinderAlone.Kept.Count(k => k)} / {generalAlone.Kept.Count(k => k)} of {generalAlone.Kept.Count} | {Robust(p, cylinderAlone):0.00} / {Robust(p, generalAlone):0.00} | {TurnDegrees(generalAlone.Model, p.Definition.Page.Width, p.Definition.Page.Height):0.0} | {SurfaceFrames.Cell(ca.Surface)} | {SurfaceFrames.Cell(ga.Surface)} | {SurfaceFrames.Cell(ga.Selected)} | {ca.Surface.ScoringOver} / {ga.Surface.ScoringOver} / {ga.Selected.ScoringOver} | {SurfaceFrames.Verdict(ca.Surface)} / {SurfaceFrames.Verdict(ga.Surface)} / {SurfaceFrames.Verdict(ga.Selected)} |");
            lines[i] = string.Create(Inv,
                $"| {gate} | `{file}` | {SurfaceFrames.Cell(whole)} | {SurfaceFrames.Cell(c.Surface)} | {SurfaceFrames.Cell(g.Surface)} | {SurfaceFrames.Cell(g.Selected)} | {whole.ScoringOver} / {c.Surface.ScoringOver} / {g.Surface.ScoringOver} / {g.Selected.ScoringOver} | {SurfaceFrames.Verdict(whole)} / {SurfaceFrames.Verdict(c.Surface)} / {SurfaceFrames.Verdict(g.Surface)} / {SurfaceFrames.Verdict(g.Selected)} |");
            shapeLines[i] = string.Create(Inv,
                $"| {gate} | `{file}` | {turn:0.0} | {fit.DeflectionDmm / 254:0.000} | {cylinder[file].Kept.Count(k => k)} / {fit.Kept.Count(k => k)} of {fit.Kept.Count} | {cylinderRobust:0.00} / {robust:0.00} | {g.Choice.F:0.0} ({g.Choice.CriticalF:0.00}) | {(g.Choice.PreferSurface ? "yes" : "no")} | {cylinderTwist.F:0.0} / {twist.F:0.0} |");
            rows[i] = new
            {
                file,
                gate,
                mapping = RawMeasurements.Mapping(fit.Mapping),
                turnAcrossPageDegrees = turn,
                deflectionIn = fit.DeflectionDmm / 254,
                corners = SurfaceFrames.CornerRows(p.Frame!, p.Fiducials!, fit),
                choice = new { g.Choice.PlanarSumSquares, g.Choice.SurfaceSumSquares, g.Choice.F, g.Choice.CriticalF, g.Choice.PreferSurface },
                robustSigmaPx = new { cylinder = cylinderRobust, general = robust },
                leftoverShape = new { cylinder = cylinderTwist, general = twist },
                alone = new
                {
                    cylinder = new { mapping = RawMeasurements.Mapping(cylinderAlone.Mapping), forwardMedianPx = Forward(p, cylinderAlone), kept = cylinderAlone.Kept.Count(k => k), bulls = RawMeasurements.Bulls(ca.SurfaceBulls, p.Definition) },
                    general = new { mapping = RawMeasurements.Mapping(generalAlone.Mapping), forwardMedianPx = Forward(p, generalAlone), kept = generalAlone.Kept.Count(k => k), bulls = RawMeasurements.Bulls(ga.SurfaceBulls, p.Definition), selected = RawMeasurements.Bulls(ga.SelectedBulls, p.Definition) },
                },
                bulls = new
                {
                    cylinder = RawMeasurements.Bulls(c.SurfaceBulls, p.Definition),
                    general = RawMeasurements.Bulls(g.SurfaceBulls, p.Definition),
                    selected = RawMeasurements.Bulls(g.SelectedBulls, p.Definition),
                },
            };
            Progress(string.Create(Inv,
                $"{file}: rulings turn {turn:0.0} degrees, {fit.Kept.Count(k => k)} of {fit.Kept.Count} corners kept, worst scoring bull cylinder {(c.Surface.WorstScoring?.Error ?? double.NaN) / 254:0.00000} in, general {(g.Surface.WorstScoring?.Error ?? double.NaN) / 254:0.00000} in, selected {(g.Selected.WorstScoring?.Error ?? double.NaN) / 254:0.00000} in, leftover-shape F {cylinderTwist.F:0.0} to {twist.F:0.0}"));
        });

        output.WriteLine();
        output.WriteLine("Worst scoring bull / worst sighter, inches: the Phase 0 whole-sheet registration, the cylinder, the general developable surface, and the model selected between the general surface and the plane. Scoring bulls over the gate and the gate verdict in the same order.");
        output.WriteLine();
        output.WriteLine("| Gate | Photograph | Whole sheet | Cylinder | General | Selected | Scoring bulls over the gate | Gate |");
        output.WriteLine("|---|---|---|---|---|---|---|---|");
        foreach (var line in lines)
        {
            output.WriteLine(line);
        }

        output.WriteLine();
        output.WriteLine("The general fit: how far its rulings turn across the page, its deflection, corners kept by the cylinder / the general surface, robust post-fit corner sigma cylinder / general, the F test of the general surface against the plane, and the leftover-shape F of M1.7 before and after (critical 6.91).");
        output.WriteLine();
        output.WriteLine("| Gate | Photograph | Rulings turn (deg) | Deflection (in) | Corners kept | Robust sigma (px per axis) | F (critical) | Bend kept | Leftover-shape F, cylinder / general |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var line in shapeLines)
        {
            output.WriteLine(line);
        }

        output.WriteLine();
        output.WriteLine("Every gated frame fitted alone, its own focal length and lens: median forward corner residual over the usable corners, corners kept, robust post-fit sigma, cylinder / general, the general fit's ruling turn, and worst scoring bull / worst sighter through the cylinder, the general surface, and the model selected between the general surface and the plane.");
        output.WriteLine();
        output.WriteLine("| Gate | Photograph | Forward residual median (px) | Corners kept | Robust sigma (px per axis) | Rulings turn (deg) | Cylinder alone | General alone | Selected | Scoring bulls over the gate | Gate |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var line in aloneLines)
        {
            output.WriteLine(line);
        }

        RawMeasurements.Write(scans, "surface-general", rows);
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"done in {clock.Elapsed.TotalMinutes:0.0} minutes"));
        return 0;
    }

    /// <summary>
    /// How far the rulings turn across the page, degrees: the largest difference in ruling direction between the spine's two
    /// ends on the page and its centre, so that a linear turn reads end to end and a quadratic one, which returns, centre to end.
    /// </summary>
    internal static double TurnDegrees(SurfaceModel model, double w, double h)
    {
        if (model.Family != SurfaceFamily.General)
        {
            return 0;
        }

        var across = new[] { new PointD(0, 0), new PointD(w, 0), new PointD(w, h), new PointD(0, h) }.Select(p => DevelopableSurface.RulingCoordinates(model, p).Across).ToList();
        double low = FoldedSheet.Theta(model, across.Min()), high = FoldedSheet.Theta(model, across.Max()), centre = FoldedSheet.Theta(model, 0);
        return new[] { Math.Abs(high - low), Math.Abs(high - centre), Math.Abs(low - centre) }.Max() * 180 / Math.PI;
    }

    private static Trial? Run(TargetDefinition definition, Cell cell, int seed, int stream)
    {
        double w = definition.Page.Width, h = definition.Page.Height;
        var matches = SyntheticSurface.Corners(definition, cell.Truth, cell.TwistDmm, cell.Noise, new Random(stream), Width, Height, cell.Subset(seed));
        if (matches.Count < 4)
        {
            return null;
        }

        var image = matches.SelectMany(m => m.ImageCorners).ToList();
        var page = matches.SelectMany(m => m.PageCorners).ToList();
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var frame = new SurfaceFrame("general", image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, ExifFocal, w / 2, h / 2), 0, 0, w, h);
        var cylinder = SurfaceFit.Fit([frame], shareCamera: false)[0];
        var general = SurfaceFit.Fit([frame], shareCamera: false, SurfaceHold.None, SurfaceFamily.General)[0];
        var choice = SurfaceSelection.Choose(general, image, page, Width, Height);
        IPageMapping selected = choice.PreferSurface || choice.Planar is null ? general.Mapping : choice.Planar;
        var bulls = definition.Bulls.Select(b =>
        {
            var declared = new PointD(b.X, b.Y);
            var at = SyntheticSurface.Image(cell.Truth, cell.TwistDmm, w, h, declared);
            return (b.Scoring, Cylinder: Dist(cylinder.Mapping.ToPage(at), declared), General: Dist(general.Mapping.ToPage(at), declared), Selected: Dist(selected.ToPage(at), declared));
        }).ToList();
        double markerSize = definition.Fiducials!.MarkerSize;
        double RobustOf(SurfaceFrameResult fit) =>
            SurfaceNoise.Measure([.. image.Select((q, k) => new SurfaceNoise.Corner(k / 4, q, fit.PageErrors[k], fit.Kept[k]))], markerSize).RobustPx;
        return new Trial(cell.Axis, cell.Level, seed, cell.Noise, matches.Count, DevelopableSurface.Deflection(cell.Truth, w, h) / 254, TurnDegrees(cell.Truth, w, h),
            bulls.Max(b => b.Cylinder) / 254, bulls.Where(b => b.Scoring).Max(b => b.Cylinder) / 254, bulls.Where(b => !b.Scoring).Max(b => b.Cylinder) / 254, cylinder.Kept.Count(k => k), RobustOf(cylinder),
            bulls.Max(b => b.General) / 254, bulls.Where(b => b.Scoring).Max(b => b.General) / 254, bulls.Where(b => !b.Scoring).Max(b => b.General) / 254, general.Kept.Count(k => k), general.Kept.Count, RobustOf(general),
            general.DeflectionDmm / 254, TurnDegrees(general.Model, w, h), general.Model.Turn ?? [], choice.F, choice.CriticalF, choice.PreferSurface,
            bulls.Max(b => b.Selected) / 254);
    }

    private static double Robust(SurfaceFrames.Prepared p, SurfaceFrameResult fit) =>
        SurfaceNoise.Measure([.. p.Frame!.Image.Select((q, k) => new SurfaceNoise.Corner(k / 4, q, fit.PageErrors[k], fit.Kept[k]))], p.Definition.Fiducials!.MarkerSize).RobustPx;

    /// <summary>The median image distance, pixels, between each usable corner and where the fitted model puts its page point.</summary>
    private static double Forward(SurfaceFrames.Prepared p, SurfaceFrameResult fit)
    {
        var frame = p.Frame!;
        var distances = Enumerable.Range(0, frame.Image.Count).Where(i => frame.Usable[i])
            .Select(i => Dist(DevelopableSurface.ToImage(fit.Model, frame.Page[i]), frame.Image[i])).ToList();
        return Percentile(distances, 0.5);
    }

    private static string Verdict((double Median, double P90) v) => v.P90 < Gate / 254 ? "pass" : v.Median < Gate / 254 ? "median passes, 90th fails" : "fail";

    private static double Percentile(IEnumerable<double> values, double q)
    {
        var sorted = values.Where(v => !double.IsNaN(v)).Order().ToList();
        return sorted.Count == 0 ? double.NaN : sorted[Math.Min(sorted.Count - 1, (int)Math.Floor((q * (sorted.Count - 1)) + 0.5))];
    }

    private static double Dist(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
