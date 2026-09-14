using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Cli.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 15 section 4: whether the lens and the bend compete for the same error, and whether a twist
/// the cylinder cannot take is what is left. <see cref="Sweep"/> is built on synthetic truth first, the main camera of
/// <see cref="SurfaceSweep"/> at a 0.40 in bow, the most the mounted frames fitted (PHASE1-RESULTS.md M1.5). It sweeps corner
/// noise from 0.5 to 5 px against marker coverage: all 34, the sets <c>main_flat2</c>, <c>main2</c> and <c>main3</c> actually
/// decoded, and 25 at random. Each is fitted with the lens free, held at truth, and held at the two extremes of the Phase 0
/// main camera's own flat-frame lens fits, which is how far a lens fitted on flat frames can be wrong. A twist series
/// calibrates <see cref="SurfaceTwist"/>. <see cref="Frames"/> then runs once on the real frames:
/// <list type="bullet">
/// <item>the main camera's lens fitted flat on its three flat frames and compared with Phase 0's range;</item>
/// <item>that lens held on the main camera's frames in the same joint fit as M1.5;</item>
/// <item>the twist diagnostic on every mounted frame, lens free and held.</item>
/// </list>
/// The ultrawide and telephoto have no flat frame, so their lens cannot be fitted without a bend, and they are not held.
/// </summary>
public static class SurfaceLens
{
    private const int Width = 3000, Height = 4000, Seeds = 10;
    private const double TruthFocal = 2600, TruthK1 = -0.05, TruthK2 = 0.065, Distance = 2600, Gate = 1.27, BowInches = 0.40;
    private static readonly double ExifFocal = 4000 * 23 / 36.0;
    private static readonly double Tilt = 8 * Math.PI / 180;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly object ProgressLock = new();

    /// <summary>The main camera's lens as Phase 0's own fits found it on its five consistent frames, NOTES-FROM-PLANNING.md entry 15 section 4.</summary>
    private const double PhaseZeroK1Low = -0.073, PhaseZeroK1High = -0.044, PhaseZeroK2Low = 0.053, PhaseZeroK2High = 0.106;

    private sealed record Lens(string Name, double? K1, double? K2);

    /// <summary>Held lenses: truth, and <c>main_flat2</c>'s and <c>main_flat3</c>'s Phase 0 fits, entry 15 section 4's table.</summary>
    private static readonly Lens[] Lenses =
    [
        new("free", null, null),
        new("held at truth", TruthK1, TruthK2),
        new("held at main_flat2's fit", -0.0436, 0.0531),
        new("held at main_flat3's fit", -0.0721, 0.1058),
    ];

    private static readonly double[] Noises = [0.5, 1, 2, 3, 5];

    private static readonly double[] Twists = [0, 0.1, 0.25, 0.5];

    private sealed record Spec(string Series, string Coverage, IReadOnlyCollection<int>? Subset, double Noise, Lens Lens, int Seed, double TwistIn, int Stream);

    private sealed record SweepTrial(string Series, string Coverage, double Noise, string Lens, int Seed, int Markers, double TwistIn, double TruthDeflectionIn,
        double FittedDeflectionIn, double FittedK1, double FittedK2, double FittedFocal, int Kept, int Corners, double SurfaceWorstIn, double SurfaceScoringIn,
        double SurfaceSighterIn, double WholeSheetWorstIn, double F, bool PreferSurface, double SelectedWorstIn, double TwistCornerIn, double TwistF, double TwistExplained);

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
        var decoded = new[] { "main_flat2.jpg", "main2.jpg", "main3.jpg" }.ToDictionary(f => f, f => SurfaceSweep.MarkersDecoded(phase0Measurements, f));
        (string Name, Func<int, IReadOnlyCollection<int>?> Subset)[] coverages =
        [
            ("all 34", _ => null),
            ($"main_flat2's {decoded["main_flat2.jpg"].Length}", _ => decoded["main_flat2.jpg"]),
            ($"main2's {decoded["main2.jpg"].Length}", _ => decoded["main2.jpg"]),
            ($"main3's {decoded["main3.jpg"].Length}", _ => decoded["main3.jpg"]),
            ("25 at random", seed =>
            {
                var random = new Random(25000 + seed);
                return ids.OrderBy(_ => random.Next()).Take(25).ToArray();
            }),
        ];

        var truth = Bow(BowInches, w, h);
        var specs = new List<Spec>();
        for (int c = 0; c < coverages.Length; c++)
        {
            for (int n = 0; n < Noises.Length; n++)
            {
                foreach (var lens in Lenses)
                {
                    for (int seed = 1; seed <= Seeds; seed++)
                    {
                        // The stream depends on coverage, noise and seed, not on the lens, so every lens sees the same corners.
                        specs.Add(new Spec("lens", coverages[c].Name, coverages[c].Subset(seed), Noises[n], lens, seed, 0, (seed * 7919) + (n * 104729) + (c * 1299709)));
                    }
                }
            }
        }

        for (int t = 0; t < Twists.Length; t++)
        {
            foreach (var lens in Lenses.Take(2))
            {
                for (int seed = 1; seed <= Seeds; seed++)
                {
                    specs.Add(new Spec("twist", "all 34", null, SyntheticSurface.MeasuredCornerNoisePixels, lens, seed, Twists[t], (seed * 7919) + (t * 15485863)));
                }
            }
        }

        Progress($"{specs.Count} synthetic trials, a {BowInches:0.00} in bow, {Seeds} seeds per cell");
        var cells = specs.Select((s, i) => (Key: CellKey(s), Index: i)).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.Select(x => x.Index).ToArray());
        var results = new SweepTrial?[specs.Count];
        var done = new ConcurrentDictionary<string, int>();
        Parallel.For(0, specs.Count, i =>
        {
            results[i] = Trial(definition, truth, specs[i]);
            string key = CellKey(specs[i]);
            if (done.AddOrUpdate(key, 1, (_, v) => v + 1) == cells[key].Length)
            {
                var cell = cells[key].Select(k => results[k]).OfType<SweepTrial>().ToList();
                Progress(string.Create(Inv, $"{key}: surface worst bull median {Percentile(cell.Select(r => r.SurfaceWorstIn), 0.5):0.00000} in, selected {Percentile(cell.Select(r => r.SelectedWorstIn), 0.5):0.00000} in, twist F median {Percentile(cell.Select(r => r.TwistF), 0.5):0.0}"));
            }
        });

        var trials = results.OfType<SweepTrial>().ToList();
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"Truth camera: {Width} by {Height}, focal {TruthFocal} px, k1 {TruthK1}, k2 {TruthK2}, {Distance / 10} mm from the page centre, tilted 8 degrees, a {BowInches:0.00} in bow with vertical rulings. {Seeds} seeds per cell. Inches."));
        output.WriteLine();
        output.WriteLine("| Coverage | Noise (px) | Lens | Trials | Markers | Surface worst bull, median / 90th pct | Surface gate | Fitted k1 / k2, median | Fitted deflection, median | Corners kept, median | Bend kept | Selected worst bull, median / 90th pct | Selected gate | Whole-sheet lens model worst, median |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var cell in trials.Where(t => t.Series == "lens").GroupBy(t => (t.Coverage, t.Noise, t.Lens)))
        {
            var c = cell.ToList();
            double median = Percentile(c.Select(t => t.SurfaceWorstIn), 0.5), p90 = Percentile(c.Select(t => t.SurfaceWorstIn), 0.9);
            double selectedMedian = Percentile(c.Select(t => t.SelectedWorstIn), 0.5), selectedP90 = Percentile(c.Select(t => t.SelectedWorstIn), 0.9);
            output.WriteLine(string.Create(Inv,
                $"| {cell.Key.Coverage} | {cell.Key.Noise:0.0} | {cell.Key.Lens} | {c.Count} | {c[0].Markers} | {median:0.00000} / {p90:0.00000} | {Verdict(median, p90)} | {Percentile(c.Select(t => t.FittedK1), 0.5):0.0000} / {Percentile(c.Select(t => t.FittedK2), 0.5):+0.0000;-0.0000} | {Percentile(c.Select(t => t.FittedDeflectionIn), 0.5):0.000} of {c[0].TruthDeflectionIn:0.000} | {Percentile(c.Select(t => (double)t.Kept), 0.5):0} of {c[0].Corners} | {c.Count(t => t.PreferSurface)} of {c.Count} | {selectedMedian:0.00000} / {selectedP90:0.00000} | {Verdict(selectedMedian, selectedP90)} | {Percentile(c.Select(t => t.WholeSheetWorstIn), 0.5):0.00000} |"));
        }

        output.WriteLine();
        output.WriteLine("The twist diagnostic on synthetic truth: the non-developable shape left out of the fitted sheet, stated at the page corner, over the same bow, all 34 markers, noise 0.52 px per axis. F critical 6.91 at p = 0.001.");
        output.WriteLine();
        output.WriteLine("| Twist put in (in) | Lens | Surface worst bull, median / 90th pct | Leftover recovered at the page corner, median (in) | F, median / 10th pct | Residual variance explained, median |");
        output.WriteLine("|---|---|---|---|---|---|");
        foreach (var cell in trials.Where(t => t.Series == "twist").GroupBy(t => (t.TwistIn, t.Lens)))
        {
            var c = cell.ToList();
            output.WriteLine(string.Create(Inv,
                $"| {cell.Key.TwistIn:0.00} | {cell.Key.Lens} | {Percentile(c.Select(t => t.SurfaceWorstIn), 0.5):0.00000} / {Percentile(c.Select(t => t.SurfaceWorstIn), 0.9):0.00000} | {Percentile(c.Select(t => t.TwistCornerIn), 0.5):+0.0000;-0.0000} | {Percentile(c.Select(t => t.TwistF), 0.5):0.0} / {Percentile(c.Select(t => t.TwistF), 0.1):0.0} | {Percentile(c.Select(t => t.TwistExplained), 0.5):0.00} |"));
        }

        RawMeasurements.Write(phase1Scans, "surface-lens-synthetic", trials);
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

        // The same fourteen photographs and grouping as grouplab surface frames, so the free fits are M1.5's.
        var samples = SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Photograph && s.Excluded is null).ToList();
        Progress($"preparing {samples.Count} photographs");
        var photos = new SurfaceFrames.Prepared[samples.Count];
        Parallel.For(0, samples.Count, i =>
        {
            photos[i] = SurfaceFrames.Prepare(scans, frozenDirectory, samples[i], new OpenCvSharpBackend());
            Progress($"prepared {photos[i].Sample.File}{(photos[i].Failure is null ? "" : ", " + photos[i].Failure)}");
        });

        var (free, _) = SurfaceFrames.FitByLens(photos, Progress);

        // The main camera's lens, fitted flat on its three flat frames with one focal length and lens shared.
        var flats = photos.Where(p => p.Sample.Gate == SampleSet.PhotographGate.Flat && p.Frame is not null).ToList();
        var flatSeed = SurfaceFit.SeedFocal([.. flats.Select(m => (m.Sample.File, m.Metadata, m.Image.Width, m.Image.Height, (Func<double, SurfaceFrame>)(f => SurfaceFrames.AtFocal(m, f))))])!;
        Progress($"fitting the main camera's lens flat on {string.Join(", ", flats.Select(f => f.Sample.File))} from {flatSeed.FocalPixels:0} px");
        var flatFits = SurfaceFit.Fit([.. flats.Select(m => SurfaceFrames.AtFocal(m, flatSeed.FocalPixels))], shareCamera: true, SurfaceHold.Flat);
        var camera = flatFits[0].Model;
        bool inside = camera.K1 >= PhaseZeroK1Low && camera.K1 <= PhaseZeroK1High && camera.K2 >= PhaseZeroK2Low && camera.K2 <= PhaseZeroK2High;
        Progress(string.Create(Inv, $"main camera lens: focal {camera.FocalPixels:0} px, k1 {camera.K1:0.0000}, k2 {camera.K2:+0.0000;-0.0000}; {(inside ? "inside" : "outside")} Phase 0's k1 {PhaseZeroK1Low} to {PhaseZeroK1High}, k2 +{PhaseZeroK2Low} to +{PhaseZeroK2High}"));
        var rows = new List<object>();
        var lensLines = new List<string>();
        for (int i = 0; i < flats.Count; i++)
        {
            var fit = flatFits[i];
            lensLines.Add(string.Create(Inv, $"| `{flats[i].Sample.File}` | {fit.Kept.Count(k => k)} of {fit.Kept.Count} | {fit.RmsKept / 254:0.00000} / {fit.RmsAll / 254:0.00000} |"));
            rows.Add(new { file = flats[i].Sample.File, condition = "the main camera's lens, fitted flat", mapping = RawMeasurements.Mapping(fit.Mapping), corners = SurfaceFrames.CornerRows(flats[i].Frame!, flats[i].Fiducials!, fit), rmsKeptIn = fit.RmsKept / 254 });
        }

        // The main camera's six frames in M1.5's joint fit, the lens held at the flat frames' lens.
        var main = photos.Where(p => p.Frame is not null && p.Metadata.FocalLengthMm == flats[0].Metadata.FocalLengthMm && p.Metadata.FNumber == flats[0].Metadata.FNumber).ToList();
        (string Name, SurfaceHold Hold)[] conditions = [("k1 and k2 held", SurfaceHold.Distortion), ("k1, k2 and focal length held", SurfaceHold.Distortion | SurfaceHold.Focal)];
        var held = new Dictionary<(string File, string Condition), SurfaceFrameResult>();
        foreach (var (name, hold) in conditions)
        {
            Progress($"fitting the main camera's frames jointly, {name}");
            var fitted = SurfaceFit.Fit([.. main.Select(m =>
            {
                var frame = SurfaceFrames.AtFocal(m, camera.FocalPixels);
                return frame with { Start = frame.Start with { K1 = camera.K1, K2 = camera.K2 } };
            })], shareCamera: true, hold);
            for (int i = 0; i < main.Count; i++)
            {
                held[(main[i].Sample.File, name)] = fitted[i];
            }
        }

        var jobs = new List<(SurfaceFrames.Prepared Photo, string Condition, SurfaceHold Hold, SurfaceFrameResult Fit)>();
        foreach (var p in photos.Where(p => p.Frame is not null && p.Sample.Gate != SampleSet.PhotographGate.None).OrderBy(p => p.Sample.Gate).ThenBy(p => p.Sample.File, StringComparer.Ordinal))
        {
            jobs.Add((p, "free", SurfaceHold.None, free[p.Sample.File]));
            foreach (var (name, hold) in conditions)
            {
                if (held.TryGetValue((p.Sample.File, name), out var fit))
                {
                    jobs.Add((p, name, hold, fit));
                }
            }
        }

        var fitLines = new string[jobs.Count];
        var twistLines = new string?[jobs.Count];
        var jobRows = new object[jobs.Count];
        Parallel.For(0, jobs.Count, j =>
        {
            var (p, condition, hold, fit) = jobs[j];
            var evaluated = SurfaceFrames.Evaluate(p, fit, hold);
            var twist = SurfaceTwist.Estimate(p.Frame!, fit.Mapping, [.. fit.PageErrors.Select(e => e <= SurfaceFit.MisreadThreshold)]);
            string gate = p.Sample.Gate == SampleSet.PhotographGate.Mounted ? "mounted" : "flat";
            var m = fit.Model;
            fitLines[j] = string.Create(Inv,
                $"| {gate} | `{p.Sample.File}` | {condition} | {m.FocalPixels:0} | {m.K1:0.0000} / {m.K2:+0.0000;-0.0000} | {fit.DeflectionDmm / 254:0.000} | {fit.Kept.Count(k => k)} of {fit.Kept.Count} | {fit.RmsKept / 254:0.00000} / {fit.RmsAll / 254:0.00000} | {evaluated.Choice.F:0.0} ({evaluated.Choice.CriticalF:0.00}) | {(evaluated.Choice.PreferSurface ? "yes" : "no")} | {SurfaceFrames.Cell(evaluated.Surface)} | {SurfaceFrames.Cell(evaluated.Selected)} | {evaluated.Surface.ScoringOver} / {evaluated.Selected.ScoringOver} | {SurfaceFrames.Verdict(evaluated.Surface)} / {SurfaceFrames.Verdict(evaluated.Selected)} |");
            if (p.Sample.Gate == SampleSet.PhotographGate.Mounted)
            {
                twistLines[j] = string.Create(Inv, $"| `{p.Sample.File}` | {condition} | {twist.Corners} | {twist.CornerOffsetDmm / 254:+0.0000;-0.0000} | {twist.AlongT:+0.0;-0.0} / {twist.SaddleT:+0.0;-0.0} | {twist.F:0.0} | {twist.VarianceExplained:0.00} |");
            }

            jobRows[j] = new
            {
                file = p.Sample.File,
                gate,
                condition,
                mapping = RawMeasurements.Mapping(fit.Mapping),
                corners = SurfaceFrames.CornerRows(p.Frame!, p.Fiducials!, fit),
                choice = new { evaluated.Choice.PlanarSumSquares, evaluated.Choice.SurfaceSumSquares, evaluated.Choice.F, evaluated.Choice.CriticalF, evaluated.Choice.PreferSurface },
                twist,
                bulls = new { surface = RawMeasurements.Bulls(evaluated.SurfaceBulls, p.Definition), selected = RawMeasurements.Bulls(evaluated.SelectedBulls, p.Definition) },
            };
            Progress(string.Create(Inv, $"{p.Sample.File}, {condition}: {fit.Kept.Count(k => k)} of {fit.Kept.Count} corners kept, deflection {fit.DeflectionDmm / 254:0.000} in, worst scoring bull {(evaluated.Surface.WorstScoring?.Error ?? double.NaN) / 254:0.00000} in surface / {(evaluated.Selected.WorstScoring?.Error ?? double.NaN) / 254:0.00000} in selected, bend {(evaluated.Choice.PreferSurface ? "kept" : "not kept")}, twist leftover at the corner {twist.CornerOffsetDmm / 254:+0.0000;-0.0000} in, F {twist.F:0.0}"));
        });

        rows.AddRange(jobRows);
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"The main camera's lens, fitted flat and shared on its flat frames: focal {camera.FocalPixels:0} px, k1 {camera.K1:0.0000}, k2 {camera.K2:+0.0000;-0.0000}, {(inside ? "inside" : "outside")} Phase 0's range."));
        output.WriteLine();
        output.WriteLine("| Flat frame | Corners kept | Residual kept / all (in) |");
        output.WriteLine("|---|---|---|");
        lensLines.ForEach(output.WriteLine);
        output.WriteLine();
        output.WriteLine("Every gated frame, lens free as M1.5 fitted it, and the main camera's frames with that lens held. Worst scoring bull / worst sighter, inches.");
        output.WriteLine();
        output.WriteLine("| Gate | Photograph | Lens | Focal (px) | k1 / k2 | Deflection (in) | Corners kept | Residual kept / all (in) | F (critical) | Bend kept | Surface | Selected | Scoring bulls over the gate: surface / selected | Gate: surface / selected |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var line in fitLines)
        {
            output.WriteLine(line);
        }

        output.WriteLine();
        output.WriteLine("The twist diagnostic on every mounted frame: the non-developable shape the fit left, at the page corner, over corners within 12.7 dmm of the fit. F critical 6.91 at p = 0.001.");
        output.WriteLine();
        output.WriteLine("| Photograph | Lens | Corners | Leftover at the page corner (in) | t along the rulings / saddle | F | Residual variance explained |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        foreach (var line in twistLines.OfType<string>())
        {
            output.WriteLine(line);
        }

        RawMeasurements.Write(scans, "surface-lens", rows);
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"done in {clock.Elapsed.TotalMinutes:0.0} minutes"));
        return 0;
    }

    private static string CellKey(Spec s) => s.Series == "twist"
        ? string.Create(Inv, $"twist {s.TwistIn:0.00} in, {s.Lens.Name}")
        : string.Create(Inv, $"{s.Coverage}, {s.Noise:0.0} px, {s.Lens.Name}");

    private static SweepTrial? Trial(TargetDefinition definition, SurfaceModel truth, Spec s)
    {
        double w = definition.Page.Width, h = definition.Page.Height, twist = s.TwistIn * 254;
        var matches = SyntheticSurface.Corners(definition, truth, twist, s.Noise, new Random(s.Stream), Width, Height, s.Subset);
        if (matches.Count < 4)
        {
            return null;
        }

        var image = matches.SelectMany(m => m.ImageCorners).ToList();
        var page = matches.SelectMany(m => m.PageCorners).ToList();
        var lensFit = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var start = SurfaceFit.StartFromLens(lensFit, ExifFocal, w / 2, h / 2);
        var hold = SurfaceHold.None;
        if (s.Lens.K1 is { } k1 && s.Lens.K2 is { } k2)
        {
            start = start with { K1 = k1, K2 = k2 };
            hold = SurfaceHold.Distortion;
        }

        var frame = new SurfaceFrame(s.Series, image, page, [.. image.Select(_ => true)], start, 0, 0, w, h);
        var fit = SurfaceFit.Fit([frame], shareCamera: false, hold)[0];
        var choice = SurfaceSelection.Choose(fit, image, page, Width, Height, hold);
        IPageMapping selected = choice.PreferSurface || choice.Planar is null ? fit.Mapping : choice.Planar;
        var bulls = definition.Bulls.Select(b =>
        {
            var declared = new PointD(b.X, b.Y);
            var at = SyntheticSurface.Image(truth, twist, w, h, declared);
            return (b.Scoring, Surface: Dist(fit.Mapping.ToPage(at), declared), Whole: Dist(lensFit.ToPage(at), declared), Selected: Dist(selected.ToPage(at), declared));
        }).ToList();
        var twistEstimate = SurfaceTwist.Estimate(frame, fit.Mapping, [.. fit.PageErrors.Select(e => e <= SurfaceFit.MisreadThreshold)]);
        return new SweepTrial(s.Series, s.Coverage, s.Noise, s.Lens.Name, s.Seed, matches.Count, s.TwistIn, DevelopableSurface.Deflection(truth, w, h) / 254,
            fit.DeflectionDmm / 254, fit.Model.K1, fit.Model.K2, fit.Model.FocalPixels, fit.Kept.Count(k => k), fit.Kept.Count,
            bulls.Max(b => b.Surface) / 254, bulls.Where(b => b.Scoring).Max(b => b.Surface) / 254, bulls.Where(b => !b.Scoring).Max(b => b.Surface) / 254,
            bulls.Max(b => b.Whole) / 254, choice.F, choice.PreferSurface, bulls.Max(b => b.Selected) / 254,
            twistEstimate.CornerOffsetDmm / 254, twistEstimate.F, twistEstimate.VarianceExplained);
    }

    /// <summary>A bow of <paramref name="inches"/> between vertical rulings, constant curvature, through the truth camera.</summary>
    internal static SurfaceModel Bow(double inches, double w, double h)
    {
        SurfaceModel Family(double c) => SyntheticSurface.Camera(Width, Height, TruthFocal, TruthK1, TruthK2, Distance, Tilt, w, h, Math.PI / 2, [0, c, 0, 0]);
        double target = inches * 254, lo = 0, hi = 0.01;
        while (DevelopableSurface.Deflection(Family(hi), w, h) < target)
        {
            hi *= 2;
        }

        for (int i = 0; i < 80; i++)
        {
            double mid = (lo + hi) / 2;
            (lo, hi) = DevelopableSurface.Deflection(Family(mid), w, h) < target ? (mid, hi) : (lo, mid);
        }

        return Family((lo + hi) / 2);
    }

    private static double Percentile(IEnumerable<double> values, double q)
    {
        var sorted = values.Where(v => !double.IsNaN(v)).Order().ToList();
        return sorted.Count == 0 ? double.NaN : sorted[Math.Min(sorted.Count - 1, (int)Math.Floor((q * (sorted.Count - 1)) + 0.5))];
    }

    private static string Verdict(double median, double p90) => p90 < Gate / 254 ? "pass" : median < Gate / 254 ? "median passes, 90th fails" : "fail";

    private static double Dist(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
