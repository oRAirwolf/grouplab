using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// PHASE1-BRIEF.md sections 3.3 step 4, 3.4 and 3.5: the developable surface fit on the real Phase 0 frames, run once after
/// the synthetic sweep, with four approaches side by side on every photograph. The whole-sheet homography with radial
/// distortion is what Phase 0 shipped. The nearest-marker diagnostic is PHASE0-RESULTS.md section 4.5's, ported: each bull's
/// centre as the whole-sheet fit located it, mapped by a homography fitted to the lens-undistorted corners of its nearest
/// k markers. The surface is the generalised cylinder, one focal length and lens shared by the frames of one pixel geometry
/// (<see cref="FitByLens"/>), all started from one focal length. The selected model is the surface where <see cref="SurfaceSelection"/> says the bend is supported and the planar
/// model otherwise. The ten gated scans go through the orthographic surface for the paper gate. Frames are prepared and
/// evaluated in parallel, each thread with its own detector, and a progress line is printed as each frame finishes
/// (NOTES-FROM-PLANNING.md entry 14).
/// </summary>
public static class SurfaceFrames
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly object ProgressLock = new();

    internal sealed record Prepared(SampleSet.Sample Sample, GrayImage Image, ImageMetadata Metadata, TargetDefinition Definition,
        FiducialResult? Fiducials, RegistrationFit? Baseline, IReadOnlyList<BullLocation> BaselineBulls, SurfaceFrame? Frame, double? ExifFocal, string? Failure,
        RadialHomographyMapping? Lens = null);

    internal sealed record Errors(string Name, IReadOnlyList<(string Label, bool Scoring, double Error)> Bulls, int Expected)
    {
        public (string Label, double Error)? WorstScoring => Bulls.Where(b => b.Scoring).OrderByDescending(b => b.Error).Select(b => ((string, double)?)(b.Label, b.Error)).FirstOrDefault();

        public (string Label, double Error)? WorstSighter => Bulls.Where(b => !b.Scoring).OrderByDescending(b => b.Error).Select(b => ((string, double)?)(b.Label, b.Error)).FirstOrDefault();

        public int ScoringOver => Bulls.Count(b => b.Scoring && b.Error >= Phase0Spike.PaperGate);

        public bool Passes => Bulls.Count == Expected && Bulls.All(b => b.Error < Phase0Spike.PaperGate);
    }

    internal sealed record Evaluation(Errors Surface, Errors Selected, SurfaceChoice Choice, IReadOnlyList<BullLocation> SurfaceBulls, IReadOnlyList<BullLocation> SelectedBulls);

    private sealed record Row(int Order, string? FitLine, string? CompareLine, string? ScanLine, object Raw);

    /// <param name="joint">
    /// Fit the frames of one pixel geometry together with one focal length and lens. Off by default: NOTES-FROM-PLANNING.md
    /// entry 17 section 4 makes one frame at a time the default, because a user photographs one target at a time and sharing
    /// a camera cost <c>main1</c> a factor of two (PHASE1-RESULTS.md M1.10). The joint fit stays for a set known to share a camera.
    /// </param>
    public static int Run(string scans, string frozenDirectory, TextWriter output, bool joint = false)
    {
        ArgumentNullException.ThrowIfNull(output);
        var clock = Stopwatch.StartNew();
        var parallel = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        void Progress(string line)
        {
            lock (ProgressLock)
            {
                output.WriteLine(string.Create(Inv, $"[{clock.Elapsed.TotalSeconds,6:0}s] {line}"));
                output.Flush();
            }
        }

        var samples = SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Photograph && s.Excluded is null)
            .OrderBy(s => s.Gate switch { SampleSet.PhotographGate.Mounted => 0, SampleSet.PhotographGate.Flat => 1, _ => 2 }).ToList();
        Progress($"preparing {samples.Count} photographs: detection, the Phase 0 whole-sheet registration and its bulls");
        var photos = new Prepared[samples.Count];
        Parallel.For(0, samples.Count, parallel, i =>
        {
            photos[i] = Prepare(scans, frozenDirectory, samples[i], new OpenCvSharpBackend());
            var p = photos[i];
            Progress(string.Create(Inv, $"prepared {p.Sample.File}: {p.Fiducials?.Matches.Count}/{p.Fiducials?.Expected} markers, EXIF focal {p.ExifFocal:0} px{(p.Failure is null ? "" : ", " + p.Failure)}"));
        });

        var (fits, seeds) = FitByLens(photos, Progress, SurfaceFamily.Cylinder, joint);

        var photoRows = new Row[photos.Length];
        Parallel.For(0, photos.Length, parallel, i => photoRows[i] = PhotoRow(i, photos[i], fits, seeds, Progress));

        var scanSamples = SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan && s.Gated).ToList();
        Progress($"the paper gate: {scanSamples.Count} scans through the orthographic surface");
        var scanRows = new Row[scanSamples.Count];
        Parallel.For(0, scanSamples.Count, parallel, i => scanRows[i] = ScanRow(i, Prepare(scans, frozenDirectory, scanSamples[i], new OpenCvSharpBackend()), Progress));

        output.WriteLine();
        output.WriteLine("Surface fit per photograph. Focal lengths in pixels: the EXIF starting estimate, the frame fitted alone, and shared across the frames of its lens. Inches.");
        output.WriteLine();
        output.WriteLine("| Gate | Lens | Photograph | Markers | EXIF focal | Focal alone / shared | Ruling angle (deg) | Deflection | Corners kept | Residual kept / all | F (critical) | Bend kept |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var r in photoRows.OrderBy(r => r.Order))
        {
            output.WriteLine(r.FitLine);
        }

        output.WriteLine();
        output.WriteLine("Worst scoring bull / worst sighter per approach, inches; scoring bulls over the gate and the gate verdict in the order the columns give.");
        output.WriteLine();
        output.WriteLine("| Gate | Photograph | Whole sheet | Nearest 6 | Nearest 8 | Surface | Selected | Scoring bulls over the gate: whole / near 6 / near 8 / surface / selected | Gate: whole / surface / selected |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var r in photoRows.OrderBy(r => r.Order).Where(r => r.CompareLine is not null))
        {
            output.WriteLine(r.CompareLine);
        }

        output.WriteLine();
        output.WriteLine("The paper gate on the ten gated scans. Worst bull, inches.");
        output.WriteLine();
        output.WriteLine("| Scan | Markers | Homography | Surface | Deflection (in) | F (critical) | Bend kept | Selected | Paper gate: homography / surface / selected |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var r in scanRows.OrderBy(r => r.Order))
        {
            output.WriteLine(r.ScanLine);
        }

        RawMeasurements.Write(scans, "surface", photoRows.OrderBy(r => r.Order).Select(r => r.Raw).Concat(scanRows.OrderBy(r => r.Order).Select(r => r.Raw)).ToList());
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"done in {clock.Elapsed.TotalMinutes:0.0} minutes"));
        return 0;
    }

    /// <summary>
    /// Every prepared photograph with a frame, fitted jointly with the others of its pixel geometry, from the one starting
    /// focal length <see cref="SurfaceFit.SeedFocal"/> chooses. NOTES-FROM-PLANNING.md entry 16 section 2: a phone that crops
    /// or zooms keeps the physical focal length and changes the 35 mm equivalent, so frames share a joint fit only when both
    /// agree, with the image size. The equivalent and size alone would not do: the Phase 0 table frames, a cropped ultrawide,
    /// share the main camera's 23 mm equivalent but not its distortion. Entry 27 section 2 adds digital zoom to the key, unknown when
    /// the tag is absent, because a phone can zoom without updating the equivalent; the key used is printed with every fit.
    /// </summary>
    internal static (Dictionary<string, SurfaceFrameResult> Fits, Dictionary<string, FocalSeed> Seeds) FitByLens(IEnumerable<Prepared> photos, Action<string> progress, SurfaceFamily family = SurfaceFamily.Cylinder, bool joint = true)
    {
        var fits = new Dictionary<string, SurfaceFrameResult>(StringComparer.Ordinal);
        var seeds = new Dictionary<string, FocalSeed>(StringComparer.Ordinal);
        foreach (var lens in photos.Where(p => p.Frame is not null).GroupBy(p => (p.Metadata.FocalLengthMm, p.Metadata.FNumber, p.Metadata.FocalLength35mm, p.Metadata.DigitalZoomRatio, p.Image.Width, p.Image.Height)))
        {
            var members = lens.ToList();
            string name = LensName(members[0]);
            progress(string.Create(Inv, $"seeding the {name} camera: {string.Join(", ", members.Select(m => m.Sample.File))}"));
            var seed = SurfaceFit.SeedFocal([.. members.Select(m => (m.Sample.File, m.Metadata, m.Image.Width, m.Image.Height, (Func<double, SurfaceFrame>)(f => AtFocal(m, f))))])!;
            progress(string.Create(Inv, $"  start {seed.FocalPixels:0} px; candidates {string.Join("; ", seed.Candidates.Select(c => $"{c.FocalPixels:0} px from {c.Frames.Count} frame(s), cost {c.Cost:0.###E+0} px^2"))}"));
            string surfaceName = family == SurfaceFamily.General ? ", general developable surface" : "";
            IReadOnlyList<SurfaceFrameResult> fitted;
            if (joint)
            {
                progress(string.Create(Inv, $"fitting the {name} camera jointly from {seed.FocalPixels:0} px{surfaceName}"));
                fitted = SurfaceFit.Fit([.. members.Select(m => AtFocal(m, seed.FocalPixels))], shareCamera: true, SurfaceHold.None, family);
            }
            else
            {
                progress(string.Create(Inv, $"fitting the {name} camera's frames one at a time from {seed.FocalPixels:0} px{surfaceName}"));
                fitted = [.. members.AsParallel().AsOrdered().Select(m => SurfaceFit.Fit([AtFocal(m, seed.FocalPixels)], shareCamera: false, SurfaceHold.None, family)[0])];
            }

            for (int i = 0; i < members.Count; i++)
            {
                fits[members[i].Sample.File] = fitted[i];
                seeds[members[i].Sample.File] = seed;
            }
        }

        return (fits, seeds);
    }

    private static string LensName(Prepared p) =>
        string.Create(Inv, $"{p.Metadata.FocalLengthMm:0.00} mm f/{p.Metadata.FNumber:0.0}, {p.Metadata.FocalLength35mm} mm equivalent, digital zoom {(p.Metadata.DigitalZoomRatio is { } zoom ? zoom.ToString("0.00", Inv) : "unknown")}, {p.Image.Width} by {p.Image.Height}");

    /// <summary>The frame started from <paramref name="focalPixels"/>: its Phase 0 lens fit's plane pose through that focal length.</summary>
    internal static SurfaceFrame AtFocal(Prepared p, double focalPixels) =>
        p.Frame! with { Start = SurfaceFit.StartFromLens(p.Lens!, focalPixels, p.Definition.Page.Width / 2.0, p.Definition.Page.Height / 2.0) };

    private static Row PhotoRow(int order, Prepared p, Dictionary<string, SurfaceFrameResult> fits, Dictionary<string, FocalSeed> seeds, Action<string> progress)
    {
        string gate = p.Sample.Gate switch { SampleSet.PhotographGate.Mounted => "mounted", SampleSet.PhotographGate.Flat => "flat", _ => "not gated" };
        string lensName = string.Create(Inv, $"{p.Metadata.FocalLengthMm:0.00} mm f/{p.Metadata.FNumber:0.0}, {p.Metadata.FocalLength35mm} mm eq., zoom {(p.Metadata.DigitalZoomRatio is { } zoom ? zoom.ToString("0.00", Inv) : "unknown")}");
        if (p.Failure is not null || !fits.TryGetValue(p.Sample.File, out var fit))
        {
            progress($"{p.Sample.File}: {p.Failure ?? "no fit"}");
            return new Row(order, $"| {gate} | {lensName} | `{p.Sample.File}` | {p.Fiducials?.Matches.Count}/{p.Fiducials?.Expected} | {p.Failure ?? "no fit"} | | | | | | | |", null, null,
                new { file = p.Sample.File, gate, failure = p.Failure ?? "no fit" });
        }

        var seed = seeds[p.Sample.File];
        var evaluated = Evaluate(p, fit);
        var (surface, selected, choice) = (evaluated.Surface, evaluated.Selected, evaluated.Choice);
        var wholeSheet = FromBulls("whole sheet", p.BaselineBulls, p.Definition);
        var near6 = Nearest(p, 6);
        var near8 = Nearest(p, 8);
        progress(string.Create(Inv,
            $"{p.Sample.File}: {p.Fiducials!.Matches.Count}/{p.Fiducials.Expected} markers, {fit.Kept.Count(k => k)} of {fit.Kept.Count} corners kept, deflection {fit.DeflectionDmm / 254:0.000} in, worst scoring bull {(surface.WorstScoring?.Error ?? double.NaN) / 254:0.00000} in surface / {(selected.WorstScoring?.Error ?? double.NaN) / 254:0.00000} in selected, F {choice.F:0.0} bend {(choice.PreferSurface ? "kept" : "not kept")}"));

        string fitLine = string.Create(Inv,
            $"| {gate} | {lensName} | `{p.Sample.File}` | {p.Fiducials.Matches.Count}/{p.Fiducials.Expected} | {p.ExifFocal:0} | {fit.IndependentFocalPixels:0} / {fit.Model.FocalPixels:0} | {Degrees(fit.Model.RulingAngle):0.0} | {fit.DeflectionDmm / 254:0.000} | {fit.Kept.Count(k => k)} of {fit.Kept.Count} | {fit.RmsKept / 254:0.00000} / {fit.RmsAll / 254:0.00000} | {choice.F:0.0} ({choice.CriticalF:0.00}) | {(choice.PreferSurface ? "yes" : "no")} |");
        string compareLine = string.Create(Inv,
            $"| {gate} | `{p.Sample.File}` | {Cell(wholeSheet)} | {Cell(near6)} | {Cell(near8)} | {Cell(surface)} | {Cell(selected)} | {wholeSheet.ScoringOver} / {near6.ScoringOver} / {near8.ScoringOver} / {surface.ScoringOver} / {selected.ScoringOver} | {Verdict(wholeSheet)} / {Verdict(surface)} / {Verdict(selected)} |");
        var raw = new
        {
            file = p.Sample.File,
            gate,
            camera = new { p.Metadata.FocalLengthMm, p.Metadata.FNumber, p.Metadata.FocalLength35mm },
            exifFocalPixels = p.ExifFocal,
            startFocalPixels = seed.FocalPixels,
            focalCandidates = seed.Candidates.Select(c => new { focalPixels = c.FocalPixels, frames = c.Frames, costPixelsSquared = double.IsNaN(c.Cost) ? (double?)null : c.Cost }).ToArray(),
            independentFocalPixels = fit.IndependentFocalPixels,
            sharedFocalPixels = fit.Model.FocalPixels,
            rulingAngleDegrees = Degrees(fit.Model.RulingAngle),
            deflectionIn = fit.DeflectionDmm / 254,
            starts = fit.Starts.Select(s => new { startDegrees = s.StartDegrees, rmsPixels = s.RmsPixels }).ToArray(),
            mapping = RawMeasurements.Mapping(fit.Mapping),
            corners = CornerRows(p.Frame!, p.Fiducials, fit),
            choice = new { choice.PlanarSumSquares, choice.SurfaceSumSquares, choice.F, choice.CriticalF, choice.PreferSurface },
            wholeSheetMapping = RawMeasurements.Mapping(p.Baseline?.Mapping),
            bulls = new
            {
                wholeSheet = RawMeasurements.Bulls(p.BaselineBulls, p.Definition),
                surface = RawMeasurements.Bulls(evaluated.SurfaceBulls, p.Definition),
                selected = RawMeasurements.Bulls(evaluated.SelectedBulls, p.Definition),
                nearest6 = near6.Bulls.Select(b => new { label = b.Label, b.Scoring, error = RawMeasurements.R(b.Error) }).ToArray(),
                nearest8 = near8.Bulls.Select(b => new { label = b.Label, b.Scoring, error = RawMeasurements.R(b.Error) }).ToArray(),
            },
        };
        return new Row(order, fitLine, compareLine, null, raw);
    }

    private static Row ScanRow(int order, Prepared p, Action<string> progress)
    {
        if (p.Failure is not null || p.Frame is null)
        {
            progress($"{p.Sample.File}: {p.Failure}");
            return new Row(order, null, null, $"| `{p.Sample.File}` | | {p.Failure} | | | | | | |", new { file = p.Sample.File, gate = "paper", failure = p.Failure });
        }

        var fit = SurfaceFit.Fit([p.Frame], shareCamera: false)[0];
        var evaluated = Evaluate(p, fit);
        var homography = FromBulls("homography", p.BaselineBulls, p.Definition);
        progress(string.Create(Inv,
            $"{p.Sample.File}: {p.Fiducials!.Matches.Count}/{p.Fiducials.Expected} markers, {fit.Kept.Count(k => k)} of {fit.Kept.Count} corners kept, deflection {fit.DeflectionDmm / 254:0.000} in, worst bull {Worst(homography)} in homography / {Worst(evaluated.Surface)} in surface / {Worst(evaluated.Selected)} in selected, F {evaluated.Choice.F:0.0} bend {(evaluated.Choice.PreferSurface ? "kept" : "not kept")}"));
        string line = string.Create(Inv,
            $"| `{p.Sample.File}` | {p.Fiducials.Matches.Count}/{p.Fiducials.Expected} | {Worst(homography)} | {Worst(evaluated.Surface)} | {fit.DeflectionDmm / 254:0.000} | {evaluated.Choice.F:0.0} ({evaluated.Choice.CriticalF:0.00}) | {(evaluated.Choice.PreferSurface ? "yes" : "no")} | {Worst(evaluated.Selected)} | {Verdict(homography)} / {Verdict(evaluated.Surface)} / {Verdict(evaluated.Selected)} |");
        var raw = new
        {
            file = p.Sample.File,
            gate = "paper",
            deflectionIn = fit.DeflectionDmm / 254,
            mapping = RawMeasurements.Mapping(fit.Mapping),
            corners = CornerRows(p.Frame, p.Fiducials, fit),
            choice = new { evaluated.Choice.PlanarSumSquares, evaluated.Choice.SurfaceSumSquares, evaluated.Choice.F, evaluated.Choice.CriticalF, evaluated.Choice.PreferSurface },
            bulls = new
            {
                homography = RawMeasurements.Bulls(p.BaselineBulls, p.Definition),
                surface = RawMeasurements.Bulls(evaluated.SurfaceBulls, p.Definition),
                selected = RawMeasurements.Bulls(evaluated.SelectedBulls, p.Definition),
            },
        };
        return new Row(order, null, null, line, raw);
    }

    internal static Prepared Prepare(string scans, string frozenDirectory, SampleSet.Sample sample, IImagingBackend backend)
    {
        var (image, metadata) = ImageLoader.Load(Path.Combine(scans, sample.File));
        var definition = Phase0Spike.Definition(frozenDirectory, sample.Definition);
        var options = new MeasureOptions();
        var trace = new TraceRecorder();
        var fiducials = SheetMeasurer.DetectFiducials(image, metadata, definition, options, backend, trace);
        if (fiducials.Matches.Count < 4)
        {
            return new Prepared(sample, image, metadata, definition, fiducials, null, [], null, null, "fewer than 4 markers");
        }

        var baseline = SheetMeasurer.Register(image, metadata, fiducials, options, backend, trace, definition: definition);
        IReadOnlyList<BullLocation> baselineBulls = baseline is null ? [] : SheetMeasurer.LocateBulls(image, definition, baseline.Mapping, options, trace);
        var imagePoints = fiducials.Matches.SelectMany(m => m.ImageCorners).ToList();
        var pagePoints = fiducials.Matches.SelectMany(m => m.PageCorners).ToList();
        var homography = backend.FindHomography(imagePoints, pagePoints, metadata.IsCamera ? SheetMeasurer.PhotographRansacThreshold : PageRegistration.RansacThreshold);
        double cx = definition.Page.Width / 2.0, cy = definition.Page.Height / 2.0;
        SurfaceModel start;
        double? exif = null;
        RadialHomographyMapping? lensFit = null;
        if (metadata.IsCamera)
        {
            exif = SurfaceFit.FocalPixelsFromExif(metadata, image.Width, image.Height);
            if (exif is null)
            {
                return new Prepared(sample, image, metadata, definition, fiducials, baseline, baselineBulls, null, null, "no 35 mm equivalent focal length tag");
            }

            var usable = homography.Inliers.ToArray();
            var lens = LensFit.Fit(imagePoints.Where((_, i) => usable[i]).ToList(), pagePoints.Where((_, i) => usable[i]).ToList(), homography.Transform, image.Width, image.Height);
            start = SurfaceFit.StartFromLens(lens, exif.Value, cx, cy);
            lensFit = lens;
        }
        else
        {
            start = SurfaceFit.StartFromHomography(homography.Transform, image.Width, image.Height, cx, cy);
        }

        var frame = new SurfaceFrame(sample.File, imagePoints, pagePoints, [.. homography.Inliers], start, 0, 0, definition.Page.Width, definition.Page.Height);
        return new Prepared(sample, image, metadata, definition, fiducials, baseline, baselineBulls, frame, exif, null, lensFit);
    }

    internal static Evaluation Evaluate(Prepared p, SurfaceFrameResult fit, SurfaceHold hold = SurfaceHold.None)
    {
        var options = new MeasureOptions();
        var trace = new TraceRecorder();
        var surfaceBulls = SheetMeasurer.LocateBulls(p.Image, p.Definition, fit.Mapping, options, trace);
        var choice = SurfaceSelection.Choose(fit, p.Frame!.Image, p.Frame.Page, p.Image.Width, p.Image.Height, hold);
        var selectedBulls = choice.PreferSurface || choice.Planar is null ? surfaceBulls : SheetMeasurer.LocateBulls(p.Image, p.Definition, choice.Planar, options, trace);
        return new Evaluation(FromBulls("surface", surfaceBulls, p.Definition), FromBulls("selected", selectedBulls, p.Definition), choice, surfaceBulls, selectedBulls);
    }

    /// <summary>PHASE0-RESULTS.md section 4.5's diagnostic, as the Phase 0 scratch analysis computed it.</summary>
    private static Errors Nearest(Prepared p, int k)
    {
        if (p.Baseline is null)
        {
            return new Errors($"nearest {k}", [], p.Definition.Bulls.Count);
        }

        Func<PointD, PointD> undistort = p.Baseline.Mapping is RadialHomographyMapping radial ? radial.Undistort : q => q;
        var inliers = p.Baseline.Corners.Where(c => c.Inlier).ToList();
        var centres = inliers.GroupBy(c => c.MarkerId).ToDictionary(g => g.Key, g => new PointD(g.Average(c => c.Page.X), g.Average(c => c.Page.Y)));
        var errors = new List<(string, bool, double)>();
        foreach (var bull in p.BaselineBulls.Where(b => b.Recovered is not null))
        {
            var near = centres.OrderBy(c => Distance(c.Value, bull.Declared)).Take(k).Select(c => c.Key).ToHashSet();
            var used = inliers.Where(c => near.Contains(c.MarkerId)).ToList();
            if (HomographyEstimate.Fit([.. used.Select(c => undistort(c.Image))], [.. used.Select(c => c.Page)]) is not { } local)
            {
                continue;
            }

            var image = p.Baseline.Mapping.ToImage(bull.Recovered!.Value);
            errors.Add((bull.Name, p.Definition.Bulls[bull.Index].Scoring, Distance(local.Apply(undistort(image)), bull.Declared)));
        }

        return new Errors($"nearest {k}", errors, p.Definition.Bulls.Count);
    }

    internal static Errors FromBulls(string name, IReadOnlyList<BullLocation> bulls, TargetDefinition definition) =>
        new(name, [.. bulls.Where(b => b.Recovered is not null).Select(b => (b.Name, definition.Bulls[b.Index].Scoring, b.Error))], definition.Bulls.Count);

    internal static object[] CornerRows(SurfaceFrame frame, FiducialResult fiducials, SurfaceFrameResult fit) =>
        [.. frame.Image.Select((q, i) => (object)new
        {
            markerId = fiducials.Matches[i / 4].Id,
            corner = i % 4,
            imageXPx = RawMeasurements.R(q.X),
            imageYPx = RawMeasurements.R(q.Y),
            pageX = frame.Page[i].X,
            pageY = frame.Page[i].Y,
            error = RawMeasurements.R(fit.PageErrors[i]),
            kept = fit.Kept[i],
        })];

    internal static string Cell(Errors e) => string.Create(Inv,
        $"{(e.WorstScoring?.Error ?? double.NaN) / 254:0.00000} / {(e.WorstSighter?.Error ?? double.NaN) / 254:0.00000}");

    private static string Worst(Errors e) => string.Create(Inv, $"{e.Bulls.Select(b => b.Error).DefaultIfEmpty(double.NaN).Max() / 254:0.00000}");

    internal static string Verdict(Errors e) => e.Passes ? "pass" : "fail";

    private static double Degrees(double radians) => ((radians * 180 / Math.PI % 180) + 180) % 180;

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
