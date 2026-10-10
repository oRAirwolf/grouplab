using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab photo-parts</c>, NOTES-FROM-PLANNING.md entry 399 section 1: where a photograph's bull-centre error comes from, before
/// anything is changed. Each photograph is registered from its own codes four ways, and its bulls located through each:
/// <list type="bullet">
/// <item>a plain homography, which is what a photograph with no camera tags gets today, because it is taken for a scan;</item>
/// <item>the homography with the two-term radial lens, the lens fit alone;</item>
/// <item>the lens fit with the bent-sheet correction of entry 324 laid over it where that correction is accepted;</item>
/// <item>the generalised cylinder of Phase 1, for the mounted frames.</item>
/// </list>
/// The final model's error at each bull is then split three ways. The codes' share is a jackknife: the registration refitted with each
/// marker left out in turn, and the spread of where it puts the bull. The registration's share is the part of the error field that is
/// smooth across the sheet, a quadratic in page position fitted to the 25 error vectors. What is left, bull by bull, is the locator's and
/// the codes'. And each worst bull is placed: its rank by local scale (1 is the side of the sheet farthest from the camera) and its
/// distance to the nearest code. Truth is a flat scan of the same sheet where one is named, the printed positions otherwise.
/// Reads only, writes nothing.
/// </summary>
public static class PhotoParts
{
    public const string Usage = "grouplab photo-parts (--scan <scan> | --sheet <definition.gltd.json>) <photograph>... | photo-parts --mounted";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly List<string> RadialLines = [];

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        var backend = new OpenCvSharpBackend();
        var photos = new List<(string Path, TargetDefinition Definition, IReadOnlyDictionary<int, PointD>? Truth)>();
        if (args is ["--residuals", .. var images])
        {
            return Residuals(images, output, backend);
        }

        if (args is ["--holes", var scanPath, .. var holePhotos])
        {
            return Holes(scanPath, holePhotos, output, error);
        }

        if (args is ["--marks", var calibreText, .. var markPhotos])
        {
            var calibre = GroupLab.Core.Marking.Calibre.Parse(calibreText, out _);
            foreach (string photo in markPhotos)
            {
                var result = AnalyzeVerb.Analyze(photo, null, out string? markFailure, null, calibre);
                var holes = result?.Automatic.Difference?.Holes ?? [];
                output.WriteLine($"{photo}: {holes.Count} marks{(markFailure is null ? "" : ", " + markFailure)}");
                foreach (var h in holes.Where(h => h.Oversized || h.PossibleMerge || h.JoinedHoles is not null || h.SizeHoles >= 1.2))
                {
                    output.WriteLine(FormattableString.Invariant(
                        $"  at {h.X:0},{h.Y:0}: size {h.SizeHoles:0.00} holes, area {h.AreaInches:0.0000} hull {h.HullAreaInches:0.0000} sq in, diameter {h.DiameterInches:0.000}, solidity {h.Solidity:0.00}, closure {h.Closure:0.00}, elongation {h.Elongation:0.00}, aspect {h.Aspect:0.00}, ink {h.InkFraction:0.00}, oversized {h.Oversized}{(h.OversizeTentative ? " (tentative)" : "")}, merge {h.PossibleMerge}, joined {h.JoinedHoles:0.00}, on ink {h.OnInk}"));
                }
            }

            return 0;
        }

        if (args is ["--mounted"])
        {
            foreach (var sample in SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Photograph && s.Gate == SampleSet.PhotographGate.Mounted && s.Excluded is null))
            {
                photos.Add((Path.Combine("scans/phase0", sample.File), Phase0Spike.Definition(SampleSet.FrozenDirectory, sample.Definition), null));
            }
        }
        else
        {
            TargetDefinition? definition = null;
            IReadOnlyDictionary<int, PointD>? truth = null;
            var files = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--scan" && i + 1 < args.Length)
                {
                    var scan = AnalyzeVerb.Analyze(args[++i], null, out string? failure);
                    if (failure is not null || scan?.Automatic.Definition is not { } d || scan.Marking is null)
                    {
                        error.WriteLine($"photo-parts: the scan could not be measured: {failure ?? scan?.Failure}");
                        return 1;
                    }

                    definition = d;
                    truth = scan.Automatic.Measurement.Bulls.Where(b => b.Recovered is not null).ToDictionary(b => b.Index, b => b.Recovered!.Value);
                }
                else if (args[i] == "--sheet" && i + 1 < args.Length)
                {
                    definition = GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(args[++i]).Definition;
                }
                else
                {
                    files.Add(args[i]);
                }
            }

            if (definition is null || files.Count == 0)
            {
                error.WriteLine(Usage);
                return 2;
            }

            photos.AddRange(files.Select(f => (f, definition, truth)));
        }

        output.WriteLine("Bull-centre error in inches against " + (photos.Any(p => p.Truth is not null) ? "the scan's bulls" : "the printed positions") + ". Worst / median per model.");
        output.WriteLine("photograph                  | homography      | lens fit        | lens + bend     | cylinder        | final: codes jk  smooth  scatter | worst bull: label scale-rank code-dist");
        foreach (var (path, definition, truth) in photos)
        {
            output.WriteLine(Row(path, definition, truth, backend));
            output.Flush();
        }

        output.WriteLine();
        foreach (string line in RadialLines)
        {
            output.WriteLine(line);
        }

        return 0;
    }

    private static string Row(string path, TargetDefinition definition, IReadOnlyDictionary<int, PointD>? truth, IImagingBackend backend)
    {
        string name = Path.GetFileName(Path.GetDirectoryName(path) is { Length: > 0 } dir && Path.GetFileName(path) == "picture.jpg" ? dir : path);
        var (image, metadata) = ImageLoader.Load(path);
        var options = new MeasureOptions();
        var trace = new TraceRecorder();
        var fiducials = SheetMeasurer.DetectFiducials(image, metadata, definition, options, backend, trace);
        if (fiducials.Matches.Count < 4)
        {
            return $"{name,-27} | fewer than 4 codes read";
        }

        var imagePoints = fiducials.Matches.SelectMany(m => m.ImageCorners).ToList();
        var pagePoints = fiducials.Matches.SelectMany(m => m.PageCorners).ToList();
        IReadOnlyDictionary<int, PointD> Truth(IReadOnlyList<BullLocation> located) =>
            truth ?? located.ToDictionary(b => b.Index, b => b.Declared);

        Dictionary<int, PointD> Locate(IPageMapping mapping) =>
            SheetMeasurer.LocateBulls(image, definition, mapping, options, new TraceRecorder()).Where(b => b.Recovered is not null).ToDictionary(b => b.Index, b => b.Recovered!.Value);

        // The four registrations, from the same codes.
        var homography = backend.FindHomography(imagePoints, pagePoints, SheetMeasurer.PhotographRansacThreshold);
        IPageMapping plain = new HomographyMapping(homography.Transform);
        IPageMapping? lens = null;
        IPageMapping? bent = null;
        bool[] kept = [.. homography.Inliers];
        try
        {
            var (radial, radialKept) = SheetMeasurer.FitLens(imagePoints, pagePoints, kept, homography.Transform, image.Width, image.Height);
            lens = radial;
            kept = radialKept;
            bent = BentSheetMapping.Fit(radial, imagePoints, pagePoints, radialKept)?.Mapping;
        }
        catch (InvalidOperationException)
        {
        }

        var cylinderFit = SheetMeasurer.Register(image, metadata, fiducials, options with { Model = RegistrationModel.Surface }, backend, new TraceRecorder(), definition: definition);
        var printed = definition.Fiducials?.Markers ?? [];
        double half = (definition.Fiducials?.MarkerSize ?? 0) / 2;
        bool Inside(Bull bull) => printed.Count == 0 || (bull.X >= printed.Min(m => m.X) - half && bull.X <= printed.Max(m => m.X) + half
            && bull.Y >= printed.Min(m => m.Y) - half && bull.Y <= printed.Max(m => m.Y) + half);
        var declared = SheetMeasurer.LocateBulls(image, definition, plain, options, new TraceRecorder());
        var expected = Truth(declared);

        string Summary(IPageMapping? mapping, out Dictionary<int, PointD>? bulls)
        {
            bulls = mapping is null ? null : Locate(mapping);
            if (bulls is null)
            {
                return "      failed    ";
            }

            var errors = Errors(bulls, expected).Select(e => e.Length).Order().ToList();
            var inside = Errors(bulls, expected).Where(e => Inside(definition.Bulls[e.Index])).Select(e => e.Length).Order().ToList();
            return errors.Count == 0 ? "      none      " : string.Create(Inv, $"{errors[^1],7:0.0000} {errors[errors.Count / 2],7:0.0000}")
                + (inside.Count < errors.Count && inside.Count > 0 ? string.Create(Inv, $" [{inside[^1]:0.0000} in codes]") : "");
        }

        string h = Summary(plain, out _);
        string l = Summary(lens, out var lensBulls);
        string b = Summary(bent ?? lens, out var bentBulls);
        string c = Summary(cylinderFit?.Mapping, out _);
        var final = bent ?? lens ?? plain;
        var finalBulls = bentBulls ?? lensBulls ?? Locate(plain);
        var errorsByBull = Errors(finalBulls, expected).ToDictionary(e => e.Index);
        if (errorsByBull.Count < 6)
        {
            return $"{name,-27} | {h} | {l} | {b} | {c} | too few bulls to split";
        }

        // The codes' share: every kept corner moved at random by the final fit's own corner residual, the registration refitted the same
        // way 30 times with a fixed seed, and the spread of where each refit puts the bull the full fit located. The residual includes
        // the model's misfit at the codes, so this is an upper bound on what the corners' reading alone costs.
        var keptCorners = Enumerable.Range(0, imagePoints.Count).Where(i => kept[i]).ToList();
        double cornerRms = Math.Sqrt(keptCorners.Average(i =>
        {
            var p = final.ToPage(imagePoints[i]);
            return Math.Pow(p.X - pagePoints[i].X, 2) + Math.Pow(p.Y - pagePoints[i].Y, 2);
        }) / 2);
        var random = new Random(399);
        double Gauss() => Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
        var shifts = new Dictionary<int, List<PointD>>();
        for (int trial = 0; trial < 30; trial++)
        {
            // The noise is drawn on the page and carried into the image through the final mapping's local scale.
            var img = imagePoints.Select(p =>
            {
                var (xx, xy, yx, yy) = final.Jacobian(p);
                double det = (xx * yy) - (xy * yx);
                double dx = Gauss() * cornerRms, dy = Gauss() * cornerRms;
                return new PointD(p.X + (((yy * dx) - (xy * dy)) / det), p.Y + (((-yx * dx) + (xx * dy)) / det));
            }).ToList();
            var pg = pagePoints.ToList();
            IPageMapping? refit;
            try
            {
                var hk = backend.FindHomography(img, pg, SheetMeasurer.PhotographRansacThreshold);
                if (lens is null)
                {
                    refit = new HomographyMapping(hk.Transform);
                }
                else
                {
                    var (rk, keptK) = SheetMeasurer.FitLens(img, pg, [.. hk.Inliers], hk.Transform, image.Width, image.Height);
                    refit = bent is null ? rk : (IPageMapping?)BentSheetMapping.Fit(rk, img, pg, keptK)?.Mapping ?? rk;
                }
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            foreach (var (index, page) in finalBulls)
            {
                var moved = refit.ToPage(final.ToImage(page));
                (shifts.TryGetValue(index, out var list) ? list : shifts[index] = []).Add(new PointD(moved.X - page.X, moved.Y - page.Y));
            }
        }

        double Jackknife(int index)
        {
            if (!shifts.TryGetValue(index, out var list) || list.Count < 2)
            {
                return double.NaN;
            }

            return Math.Sqrt(list.Average(p => (p.X * p.X) + (p.Y * p.Y))) / 254;
        }

        // The registration's share: the quadratic field through the error vectors; the scatter is what it leaves, bull by bull.
        var indices = errorsByBull.Keys.Order().ToList();
        var field = SmoothField(indices.Select(i => expected[i]).ToList(), indices.Select(i => errorsByBull[i].Vector).ToList());
        var smooth = indices.ToDictionary(i => i, i => field(expected[i]));
        var scatter = indices.ToDictionary(i => i, i => new PointD(errorsByBull[i].Vector.X - smooth[i].X, errorsByBull[i].Vector.Y - smooth[i].Y));
        static double Length(PointD p) => Math.Sqrt((p.X * p.X) + (p.Y * p.Y)) / 254;
        static double Rms(IEnumerable<double> values)
        {
            var list = values.Where(double.IsFinite).ToList();
            return list.Count == 0 ? double.NaN : Math.Sqrt(list.Average(v => v * v));
        }

        int worst = indices.MaxBy(i => errorsByBull[i].Length);

        // Where it sits: local scale (pixels per page dmm, from the final mapping's Jacobian) ranked from the smallest, the far side,
        // and the distance to the nearest code's centre.
        var scale = indices.ToDictionary(i => i, i =>
        {
            var (xx, xy, yx, yy) = final.Jacobian(final.ToImage(expected[i]));
            return 1 / Math.Sqrt(Math.Abs((xx * yy) - (xy * yx)));
        });
        int rank = indices.OrderBy(i => scale[i]).ToList().IndexOf(worst) + 1;
        var codeCentres = fiducials.Matches.Select(m => new PointD(m.PageCorners.Average(p => p.X), m.PageCorners.Average(p => p.Y))).ToList();
        double codeDistance = codeCentres.Min(cc => Math.Sqrt(Math.Pow(cc.X - expected[worst].X, 2) + Math.Pow(cc.Y - expected[worst].Y, 2))) / 254;
        string label = definition.Bulls[worst].Label ?? worst.ToString(Inv);
        double rankCorrelation = Spearman(indices.Select(i => errorsByBull[i].Length).ToList(), indices.Select(i => -scale[i]).ToList());
        double codeCorrelation = Spearman(indices.Select(i => errorsByBull[i].Length).ToList(),
            indices.Select(i => codeCentres.Min(cc => Math.Sqrt(Math.Pow(cc.X - expected[i].X, 2) + Math.Pow(cc.Y - expected[i].Y, 2)))).ToList());

        // Lens or relief: each error split along the image's radius at the bull (from the image centre, carried onto the page by the
        // mapping's Jacobian) and across it, and the radial part by the bull's distance from the image centre over the half diagonal.
        // A lens profile the two terms miss is the same function of that distance in every photograph of one camera; paper relief seen
        // through a wide lens is radial too, but differs from photograph to photograph.
        var centre = new PointD(image.Width / 2.0, image.Height / 2.0);
        double halfDiagonal = Math.Sqrt((image.Width * image.Width) + (image.Height * image.Height)) / 2;
        var radialErrors = indices.Select(i =>
        {
            var at = final.ToImage(expected[i]);
            double ux = at.X - centre.X, uy = at.Y - centre.Y, ul = Math.Sqrt((ux * ux) + (uy * uy));
            var (xx, xy, yx, yy) = final.Jacobian(at);
            double dx = (xx * ux) + (xy * uy), dy = (yx * ux) + (yy * uy), dl = Math.Sqrt((dx * dx) + (dy * dy));
            var e = errorsByBull[i].Vector;
            return (Rho: ul / halfDiagonal, Radial: dl == 0 ? 0 : ((e.X * dx) + (e.Y * dy)) / dl / 254, Total: errorsByBull[i].Length);
        }).ToList();
        double radialShare = radialErrors.Sum(r => r.Radial * r.Radial) / Math.Max(1e-12, radialErrors.Sum(r => r.Total * r.Total));
        string profile = string.Join(" ", radialErrors.GroupBy(r => Math.Min(4, (int)(r.Rho * 5))).OrderBy(g => g.Key)
            .Select(g => FormattableString.Invariant($"{g.Key * 0.2:0.0}-{(g.Key + 1) * 0.2:0.0}:{g.Average(r => r.Radial) * 1000:+0.0;-0.0}({g.Count()})")));
        RadialLines.Add(FormattableString.Invariant($"{name,-27} radial share {radialShare:P0}; mean radial error, thousandths of an inch, by distance from the image centre: {profile}"));

        return FormattableString.Invariant(
                $"{name,-27} | {h} | {l} | {b} | {c} | rms {Rms(indices.Select(Jackknife)),6:0.0000} {Rms(smooth.Values.Select(Length)),6:0.0000} {Rms(scatter.Values.Select(Length)),6:0.0000}")
            + FormattableString.Invariant($"  at worst {Jackknife(worst),6:0.0000} {Length(smooth[worst]),6:0.0000} {Length(scatter[worst]),6:0.0000}")
            + FormattableString.Invariant($" | {label} {errorsByBull[worst].Length:0.0000} rank {rank}/{indices.Count} code {codeDistance:0.00} in; error~far {rankCorrelation:+0.00;-0.00} error~code-dist {codeCorrelation:+0.00;-0.00}")
            + FormattableString.Invariant($" | {fiducials.Matches.Count} codes, corner rms {cornerRms / 254:0.0000} in, final {final.Model}");
    }

    /// <summary>
    /// For each image, identified as the application identifies it: whether it states a resolution and carries a focal length, and the codes'
    /// corner residual under a plain homography and under the lens fit, so a rule can tell a photograph from a scan by the fit.
    /// </summary>
    private static int Residuals(IReadOnlyList<string> images, TextWriter output, IImagingBackend backend)
    {
        output.WriteLine("image | dpi | tagged | codes | homography rms, all corners | lens rms, kept | kept | ratio | lens edge px");
        foreach (string path in images)
        {
            var result = AnalyzeVerb.Analyze(path, null, out string? failure);
            if (failure is not null || result?.Automatic.Definition is not { } definition)
            {
                output.WriteLine($"{path} | not identified: {failure ?? result?.Failure}");
                continue;
            }

            var (image, metadata) = ImageLoader.Load(path);
            var fiducials = SheetMeasurer.DetectFiducials(image, metadata, definition, new MeasureOptions(), backend, new TraceRecorder());
            if (fiducials.Matches.Count < 4)
            {
                output.WriteLine($"{path} | {fiducials.Matches.Count} codes");
                continue;
            }

            var imagePoints = fiducials.Matches.SelectMany(m => m.ImageCorners).ToList();
            var pagePoints = fiducials.Matches.SelectMany(m => m.PageCorners).ToList();
            var homography = backend.FindHomography(imagePoints, pagePoints, SheetMeasurer.PhotographRansacThreshold);
            var plain = new HomographyMapping(homography.Transform);
            double All(IPageMapping m, IReadOnlyList<bool> keep) => Math.Sqrt(Enumerable.Range(0, imagePoints.Count).Where(i => keep[i]).Average(i =>
            {
                var p = m.ToPage(imagePoints[i]);
                return Math.Pow(p.X - pagePoints[i].X, 2) + Math.Pow(p.Y - pagePoints[i].Y, 2);
            })) / 254;
            double h = All(plain, [.. imagePoints.Select(_ => true)]);
            string lensPart;
            try
            {
                var (lens, kept) = SheetMeasurer.FitLens(imagePoints, pagePoints, [.. homography.Inliers], homography.Transform, image.Width, image.Height);
                double l = All(lens, kept);
                var corner = new PointD(0, 0);
                var undistorted = lens.ToImage(plain.ToPage(corner));
                double edge = Math.Sqrt(Math.Pow(undistorted.X - corner.X, 2) + Math.Pow(undistorted.Y - corner.Y, 2));
                int keptCount = kept.Count(k => k);
                lensPart = FormattableString.Invariant($"{l:0.00000} | {keptCount}/{kept.Length} | {h / Math.Max(l, 1e-9):0.0} | {edge:0.0}");
            }
            catch (InvalidOperationException ex)
            {
                lensPart = "failed: " + ex.Message;
            }

            output.WriteLine(FormattableString.Invariant($"{Path.GetFileName(path)} | {metadata.DpiX?.ToString("0", Inv) ?? "none"} | {(metadata.IsCamera ? "yes" : "no")} | {fiducials.Matches.Count} | {h:0.00000} | {lensPart}"));
            output.Flush();
        }

        return 0;
    }

    /// <summary>
    /// Holes, the measure that is not circular: each photograph's holes against the scan's, as the application reads them, three ways. Placed
    /// on the page (what <c>compare-photos</c> reports); measured from their own bull, the offset a shooter reads; and placed after a
    /// correction anchored on the bulls, which pulls each located bull to its printed position and leaves the codes where they are, smoothly
    /// between (a Gaussian-weighted average of the bulls' corrections and the codes' zeros, sigma half the bulls' spacing). The anchoring
    /// uses the printed positions only, never the scan.
    /// </summary>
    private static int Holes(string scanPath, IReadOnlyList<string> photos, TextWriter output, TextWriter error)
    {
        var scan = AnalyzeVerb.Analyze(scanPath, null, out string? failure);
        if (failure is not null || scan?.Marking is not { } scanMarking || scan.Automatic.Scale is not { } scanScale)
        {
            error.WriteLine($"photo-parts: the scan could not be measured: {failure ?? scan?.Failure}");
            return 1;
        }

        var truthHoles = scanMarking.Shots.Where(s => s.IsShot).Select(s => scanScale.Mapping.ToPage(s.Image)).ToList();
        var truthBulls = scan.Automatic.Measurement.Bulls.Where(b => b.Recovered is not null).ToDictionary(b => b.Index, b => b.Recovered!.Value);
        output.WriteLine("Hole error in inches against the scan: median / p95 / worst. Placed; from its own bull; placed after anchoring on the bulls.");
        foreach (string photo in photos)
        {
            var result = AnalyzeVerb.Analyze(photo, null, out string? photoFailure);
            if (photoFailure is not null || result?.Marking is not { } marking || result.Automatic.Scale is not { } scale)
            {
                output.WriteLine($"{photo}: failed");
                continue;
            }

            var bulls = result.Automatic.Measurement.Bulls.Where(b => b.Recovered is not null).ToList();
            var located = bulls.ToDictionary(b => b.Index, b => b.Recovered!.Value);
            var holes = marking.Shots.Where(s => s.IsShot).Select(s => scale.Mapping.ToPage(s.Image)).ToList();
            var codes = result.Automatic.Measurement.Fiducials?.Matches.Select(m => new PointD(m.PageCorners.Average(p => p.X), m.PageCorners.Average(p => p.Y))).ToList() ?? [];
            double spacing = bulls.Count > 1 ? bulls.Average(b => bulls.Where(o => o != b).Min(o => Dist(o.Declared, b.Declared))) : 254;
            double sigma = spacing / 2;
            var anchors = bulls.Select(b => (At: b.Recovered!.Value, Shift: new PointD(b.Declared.X - b.Recovered!.Value.X, b.Declared.Y - b.Recovered!.Value.Y)))
                .Concat(codes.Select(c => (At: c, Shift: new PointD(0, 0)))).ToList();
            PointD Anchor(PointD p)
            {
                double wx = 0, wy = 0, w = 0;
                foreach (var (at, shift) in anchors)
                {
                    double k = Math.Exp(-Math.Pow(Dist(at, p), 2) / (2 * sigma * sigma));
                    wx += k * shift.X;
                    wy += k * shift.Y;
                    w += k;
                }

                return w == 0 ? p : new PointD(p.X + (wx / w), p.Y + (wy / w));
            }

            var placed = new List<double>();
            var relative = new List<double>();
            var anchored = new List<double>();
            foreach (var (t, h) in Pair(truthHoles, holes))
            {
                placed.Add(Dist(t, h) / 254);
                int bull = truthBulls.Keys.Where(located.ContainsKey).MinBy(k => Dist(truthBulls[k], t));
                var tr = new PointD(t.X - truthBulls[bull].X, t.Y - truthBulls[bull].Y);
                var hr = new PointD(h.X - located[bull].X, h.Y - located[bull].Y);
                relative.Add(Dist(tr, hr) / 254);
                anchored.Add(Dist(t, Anchor(h)) / 254);
            }

            output.WriteLine(FormattableString.Invariant($"{Path.GetFileName(Path.GetDirectoryName(photo))}/{Path.GetFileName(photo),-22} {scale.Mapping.Model,-52} {placed.Count,2} holes | {Stats(placed)} | {Stats(relative)} | {Stats(anchored)}"));
            output.Flush();
        }

        return 0;
    }

    private static double Dist(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private static string Stats(List<double> values)
    {
        if (values.Count == 0)
        {
            return "none";
        }

        var v = values.Order().ToList();
        return FormattableString.Invariant($"{v[v.Count / 2]:0.0000} {v[Math.Clamp((int)Math.Ceiling(0.95 * v.Count) - 1, 0, v.Count - 1)]:0.0000} {v[^1]:0.0000}");
    }

    /// <summary>Nearest first, each at most once, within the hole-matching threshold, as <see cref="PhotoComparison"/> pairs them.</summary>
    private static List<(PointD Truth, PointD Photo)> Pair(IReadOnlyList<PointD> truth, IReadOnlyList<PointD> photo)
    {
        var pairs = new List<(int T, int P, double D)>();
        for (int t = 0; t < truth.Count; t++)
        {
            for (int p = 0; p < photo.Count; p++)
            {
                double d = Dist(truth[t], photo[p]) / 254;
                if (d <= PhotoComparison.HoleMatchInches)
                {
                    pairs.Add((t, p, d));
                }
            }
        }

        var usedT = new HashSet<int>();
        var usedP = new HashSet<int>();
        var result = new List<(PointD, PointD)>();
        foreach (var (t, p, _) in pairs.OrderBy(x => x.D))
        {
            if (!usedT.Contains(t) && !usedP.Contains(p))
            {
                usedT.Add(t);
                usedP.Add(p);
                result.Add((truth[t], photo[p]));
            }
        }

        return result;
    }

    private static IEnumerable<(int Index, PointD Vector, double Length)> Errors(IReadOnlyDictionary<int, PointD> bulls, IReadOnlyDictionary<int, PointD> truth) =>
        bulls.Where(b => truth.ContainsKey(b.Key)).Select(b =>
        {
            var v = new PointD(b.Value.X - truth[b.Key].X, b.Value.Y - truth[b.Key].Y);
            return (b.Key, v, Math.Sqrt((v.X * v.X) + (v.Y * v.Y)) / 254);
        });

    /// <summary>A quadratic in page position per axis, least squares, with the page centred and scaled so the normal equations are sound.</summary>
    private static Func<PointD, PointD> SmoothField(IReadOnlyList<PointD> at, IReadOnlyList<PointD> vectors)
    {
        double cx = at.Average(p => p.X), cy = at.Average(p => p.Y);
        double s = Math.Max(1, at.Max(p => Math.Max(Math.Abs(p.X - cx), Math.Abs(p.Y - cy))));
        double[] Terms(PointD p)
        {
            double x = (p.X - cx) / s, y = (p.Y - cy) / s;
            return [1, x, y, x * x, x * y, y * y];
        }

        double[] Solve(Func<PointD, double> component)
        {
            int n = 6;
            var a = new double[n, n + 1];
            for (int k = 0; k < at.Count; k++)
            {
                var t = Terms(at[k]);
                double v = component(vectors[k]);
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        a[i, j] += t[i] * t[j];
                    }

                    a[i, n] += t[i] * v;
                }
            }

            for (int col = 0; col < n; col++)
            {
                int pivot = Enumerable.Range(col, n - col).MaxBy(r => Math.Abs(a[r, col]));
                for (int j = 0; j <= n; j++)
                {
                    (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);
                }

                for (int r = 0; r < n; r++)
                {
                    if (r != col && a[col, col] != 0)
                    {
                        double f = a[r, col] / a[col, col];
                        for (int j = col; j <= n; j++)
                        {
                            a[r, j] -= f * a[col, j];
                        }
                    }
                }
            }

            return [.. Enumerable.Range(0, n).Select(i => a[i, i] == 0 ? 0 : a[i, n] / a[i, i])];
        }

        var bx = Solve(v => v.X);
        var by = Solve(v => v.Y);
        return p =>
        {
            var t = Terms(p);
            return new PointD(t.Zip(bx).Sum(z => z.First * z.Second), t.Zip(by).Sum(z => z.First * z.Second));
        };
    }

    private static double Spearman(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        static double[] Ranks(IReadOnlyList<double> v)
        {
            var order = Enumerable.Range(0, v.Count).OrderBy(i => v[i]).ToList();
            var r = new double[v.Count];
            for (int k = 0; k < order.Count; k++)
            {
                r[order[k]] = k;
            }

            return r;
        }

        var ra = Ranks(a);
        var rb = Ranks(b);
        double ma = ra.Average(), mb = rb.Average();
        double cov = ra.Zip(rb).Sum(z => (z.First - ma) * (z.Second - mb));
        double va = Math.Sqrt(ra.Sum(x => (x - ma) * (x - ma))), vb = Math.Sqrt(rb.Sum(x => (x - mb) * (x - mb)));
        return va == 0 || vb == 0 ? 0 : cov / (va * vb);
    }
}
