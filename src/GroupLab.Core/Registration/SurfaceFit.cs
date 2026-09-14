using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// One image's marker corners for a surface fit, with the starting model and the corners that may be used: for a
/// photograph the first RANSAC's inliers, which reject misreads but not a bend, and for a scan the scan's inliers.
/// <see cref="PageLeft"/> to <see cref="PageBottom"/> bound the page, for the mapping's starting homography and the
/// deflection report.
/// </summary>
public sealed record SurfaceFrame(
    string Name,
    IReadOnlyList<PointD> Image,
    IReadOnlyList<PointD> Page,
    IReadOnlyList<bool> Usable,
    SurfaceModel Start,
    double PageLeft,
    double PageTop,
    double PageRight,
    double PageBottom);

/// <summary>A fitted frame: the model and mapping, each corner's page error in dmm, and which corners the fit kept.</summary>
public sealed record SurfaceFrameResult(
    SurfaceModel Model,
    SurfaceMapping Mapping,
    IReadOnlyList<double> PageErrors,
    IReadOnlyList<bool> Kept,
    double RmsKept,
    double RmsAll,
    double MaxKept,
    double IndependentFocalPixels,
    double DeflectionDmm,
    IReadOnlyList<(double StartDegrees, double RmsPixels)> Starts);

/// <summary>
/// Parameters a surface fit holds at its starting model's values instead of fitting. NOTES-FROM-PLANNING.md entry 15
/// section 4: on a bent sheet the radial lens term can absorb the bend, so the lens is a property of the camera, fitted
/// where it is well determined and held while the bend is fitted. <see cref="Flat"/> holds the ruling angle and bend at
/// zero, a flat sheet through the same camera model, which is how that lens is fitted on flat frames.
/// </summary>
[Flags]
public enum SurfaceHold
{
    None = 0,

    /// <summary>k1 and k2.</summary>
    Distortion = 1,

    /// <summary>The focal length of a perspective model.</summary>
    Focal = 2,

    /// <summary>The ruling angle and every bend coefficient, at zero.</summary>
    Flat = 4,
}

/// <summary>A starting focal length, pixels, that some frames' EXIF gives; those frames; and the summed squared residual, pixels squared, of every frame fitted alone from it, NaN when it was the only start.</summary>
public sealed record FocalCandidate(double FocalPixels, IReadOnlyList<string> Frames, double Cost);

/// <summary>A joint fit's one starting focal length and the candidates weighed (<see cref="SurfaceFit.SeedFocal"/>).</summary>
public sealed record FocalSeed(double FocalPixels, IReadOnlyList<FocalCandidate> Candidates);

/// <summary>
/// The developable surface fit of PHASE1-BRIEF.md M1. Each frame is fitted alone from several ruling angles, because a
/// ruling angle is undefined until the sheet bends, and the best start is kept. Every corner is then reclassified, first at
/// <see cref="MisreadThreshold"/> and then at the Phase 0 inlier distance, with a refit after each, so corners the planar
/// RANSAC rejected because the sheet bends are taken back. Frames that share a lens, by <see cref="Fit"/>'s caller, are then fitted
/// together with one focal length and one lens, per section 3.2, and reclassified and refitted once more. Residuals are
/// in pixels, measured in undistorted normalised coordinates and scaled back by the normalisation, because a detector's
/// corner error is a pixel quantity.
/// </summary>
public static class SurfaceFit
{
    /// <summary>Bend coefficients fitted: tangent angle to the cube of arc length, so curvature is a quadratic.</summary>
    public const int BendTerms = 3;

    /// <summary>Starting ruling angles, degrees.</summary>
    public static IReadOnlyList<double> StartAngles { get; } = [0, 30, 60, 90, 120, 150];

    private const int FrameParameters = 4 + 6;

    /// <summary>
    /// The first reclassification distance, page dmm: a photograph's first RANSAC distance, which rejects misreads only
    /// (<c>SheetMeasurer.PhotographRansacThreshold</c>). Reclassifying straight to the Phase 0 inlier distance lost whole
    /// marker columns on a rendered 1.00 in bow: the planar RANSAC had rejected them before the surface fit saw them, the
    /// bend fitted to the remaining columns missed them by 4 to 5 dmm, and 96 of 136 corners were kept (PHASE1-RESULTS.md M1).
    /// </summary>
    public const double MisreadThreshold = 12.7;

    /// <summary>
    /// PHASE1-BRIEF.md section 3.2's starting focal length, pixels: the 35 mm equivalent over 36 mm, times the image's long
    /// side. A starting value only; the fit refines it. Null when the image carries no such tag, and such an image cannot
    /// use this model at all.
    /// </summary>
    public static double? FocalPixelsFromExif(ImageMetadata metadata, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return metadata.FocalLength35mm is { } equivalent ? equivalent / 36.0 * Math.Max(width, height) : null;
    }

    /// <summary>
    /// A perspective starting model from a Phase 0 lens fit and a focal length: the plane pose that reproduces the lens
    /// fit's homography through that focal length, flat, with the lens fit's distortion.
    /// </summary>
    public static SurfaceModel StartFromLens(RadialHomographyMapping lens, double focalPixels, double pageCentreX, double pageCentreY)
    {
        ArgumentNullException.ThrowIfNull(lens);
        var toCentredPage = new Homography([1, 0, pageCentreX, 0, 1, pageCentreY, 0, 0, 1]);
        var g = Homography.Compose(toCentredPage, lens.NormalisedToPage.Inverse());
        double f = focalPixels / lens.Scale;
        double[] m1 = [g[0, 0] / f, g[1, 0] / f, g[2, 0]];
        double[] m2 = [g[0, 1] / f, g[1, 1] / f, g[2, 1]];
        double[] m3 = [g[0, 2] / f, g[1, 2] / f, g[2, 2]];
        double scale = 2 / (Norm(m1) + Norm(m2));
        if (scale * m3[2] < 0)
        {
            scale = -scale;
        }

        double[] r1 = [.. m1.Select(v => v * scale)];
        double[] r2 = [.. m2.Select(v => v * scale)];
        double[] r3 = [(r1[1] * r2[2]) - (r1[2] * r2[1]), (r1[2] * r2[0]) - (r1[0] * r2[2]), (r1[0] * r2[1]) - (r1[1] * r2[0])];
        var rotation = Orthonormalise(new double[,] { { r1[0], r2[0], r3[0] }, { r1[1], r2[1], r3[1] }, { r1[2], r2[2], r3[2] } });
        var w = DevelopableSurface.RotationVector(rotation);
        return new SurfaceModel(SurfaceProjection.Perspective, 0, new double[BendTerms + 1], w.X, w.Y, w.Z,
            m3[0] * scale, m3[1] * scale, m3[2] * scale, f, lens.K1, lens.K2, lens.CentreX, lens.CentreY, lens.Scale, pageCentreX, pageCentreY);
    }

    /// <summary>An orthographic starting model from a scan's homography: its scale and in-plane rotation at the page centre.</summary>
    public static SurfaceModel StartFromHomography(Homography imageToPage, int width, int height, double pageCentreX, double pageCentreY)
    {
        ArgumentNullException.ThrowIfNull(imageToPage);
        double cx = (width - 1) / 2.0, cy = (height - 1) / 2.0, s = Math.Max(width, height) / 2.0;
        var toImage = imageToPage.Inverse();
        PointD N(PointD page)
        {
            var q = toImage.Apply(page);
            return new PointD((q.X - cx) / s, (q.Y - cy) / s);
        }

        const double h = 10;
        var o = N(new PointD(pageCentreX, pageCentreY));
        var px = N(new PointD(pageCentreX + h, pageCentreY));
        var py = N(new PointD(pageCentreX, pageCentreY + h));
        double jxx = (px.X - o.X) / h, jyx = (px.Y - o.Y) / h, jxy = (py.X - o.X) / h, jyy = (py.Y - o.Y) / h;
        double alpha = Math.Sqrt(Math.Abs((jxx * jyy) - (jxy * jyx)));
        double gamma = Math.Atan2(jyx - jxy, jxx + jyy);
        return new SurfaceModel(SurfaceProjection.Orthographic, 0, new double[BendTerms + 1], 0, 0, gamma, o.X, o.Y, 0, alpha, 0, 0, cx, cy, s, pageCentreX, pageCentreY);
    }

    /// <param name="frames">Frames to fit.</param>
    /// <param name="shareCamera">Fit one focal length and lens across all the frames, which must be perspective and share a lens.</param>
    /// <param name="hold">Parameters held at each frame's starting values (<see cref="SurfaceHold"/>).</param>
    public static IReadOnlyList<SurfaceFrameResult> Fit(IReadOnlyList<SurfaceFrame> frames, bool shareCamera, SurfaceHold hold = SurfaceHold.None)
    {
        ArgumentNullException.ThrowIfNull(frames);
        var models = new SurfaceModel[frames.Count];
        var kept = new bool[frames.Count][];
        var independentFocal = new double[frames.Count];
        var starts = new List<(double, double)>[frames.Count];
        for (int f = 0; f < frames.Count; f++)
        {
            var frame = frames[f];
            var usable = frame.Usable.ToArray();
            starts[f] = [];
            var (refined, _) = BestStart(frame, usable, starts[f], hold);
            foreach (double threshold in (double[])[MisreadThreshold, PageRegistration.RansacThreshold])
            {
                kept[f] = Reclassify(frame, refined, threshold);
                refined = FitAlone(frame, refined, kept[f], hold).Model;
            }

            models[f] = refined;
            independentFocal[f] = refined.FocalPixels;
        }

        if (shareCamera && frames.Count > 1 && frames.All(x => x.Start.Projection == SurfaceProjection.Perspective))
        {
            models = FitJointly(frames, models, kept, hold);
            for (int f = 0; f < frames.Count; f++)
            {
                kept[f] = Reclassify(frames[f], models[f], PageRegistration.RansacThreshold);
            }

            models = FitJointly(frames, models, kept, hold);
        }

        var results = new List<SurfaceFrameResult>(frames.Count);
        for (int f = 0; f < frames.Count; f++)
        {
            var frame = frames[f];
            var mapping = new SurfaceMapping(models[f], frame.PageLeft, frame.PageTop, frame.PageRight, frame.PageBottom);
            var errors = frame.Image.Select((p, i) => Distance(mapping.ToPage(p), frame.Page[i])).ToList();
            var k = errors.Select(e => e <= PageRegistration.RansacThreshold).ToList();
            double rmsKept = Math.Sqrt(errors.Where((e, i) => k[i]).Select(e => e * e).DefaultIfEmpty(double.NaN).Average());
            double rmsAll = Math.Sqrt(errors.Average(e => e * e));
            double maxKept = errors.Where((e, i) => k[i]).DefaultIfEmpty(double.NaN).Max();
            double deflection = DevelopableSurface.Deflection(models[f] with { PageCentreX = models[f].PageCentreX, PageCentreY = models[f].PageCentreY },
                frame.PageRight - frame.PageLeft, frame.PageBottom - frame.PageTop);
            results.Add(new SurfaceFrameResult(models[f], mapping, errors, k, rmsKept, rmsAll, maxKept, independentFocal[f], deflection, starts[f]));
        }

        return results;
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 15 section 1: frames fitted jointly share one lens, so they share one starting focal
    /// length and never take one each. The EXIF offers a start per frame from the 35 mm equivalent. A caller that groups by
    /// pixel geometry, as <c>grouplab surface frames</c> does, gives this one candidate; a group whose equivalents differ gets
    /// each distinct start tried on every frame, fitted alone from every starting ruling angle over the frame's usable
    /// corners, which do not depend on the start, and the start with the least total squared residual is the group's. A
    /// difference is not flagged: a phone that crops or zooms keeps the physical focal length and changes the equivalent, so
    /// the two tags describe different things and both are true (entry 16 section 2). Null when no frame carries a 35 mm
    /// equivalent.
    /// </summary>
    /// <param name="frames">Each frame's name and metadata, image size, and the frame built at a given starting focal length.</param>
    public static FocalSeed? SeedFocal(IReadOnlyList<(string Name, ImageMetadata Metadata, int Width, int Height, Func<double, SurfaceFrame> FrameAt)> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        var exif = frames.Select(f => FocalPixelsFromExif(f.Metadata, f.Width, f.Height)).ToList();
        var candidates = exif.Where(e => e is not null).Select(e => Math.Round(e!.Value, 1)).Distinct().Order().ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var costs = new double[candidates.Count];
        if (candidates.Count > 1)
        {
            var each = new double[candidates.Count * frames.Count];
            Parallel.For(0, each.Length, k =>
            {
                var frame = frames[k % frames.Count].FrameAt(candidates[k / frames.Count]);
                each[k] = BestStart(frame, [.. frame.Usable], null, SurfaceHold.None).Cost;
            });
            for (int c = 0; c < candidates.Count; c++)
            {
                costs[c] = Enumerable.Range(0, frames.Count).Sum(f => each[(c * frames.Count) + f]);
            }
        }

        int chosen = Array.IndexOf(costs, costs.Min());
        double focal = candidates[chosen];
        return new FocalSeed(focal, [.. candidates.Select((c, i) => new FocalCandidate(c, [.. frames.Where((_, f) => exif[f] is { } e && Math.Round(e, 1) == c).Select(x => x.Name)], candidates.Count > 1 ? costs[i] : double.NaN))]);
    }

    /// <summary>
    /// The frame fitted alone from every starting ruling angle, flat, over <paramref name="use"/>: the best model and its
    /// cost. A flat hold has no ruling angle to search, so it starts once.
    /// </summary>
    private static (SurfaceModel Model, double Cost) BestStart(SurfaceFrame frame, bool[] use, List<(double, double)>? starts, SurfaceHold hold)
    {
        SurfaceModel? best = null;
        double bestCost = double.PositiveInfinity;
        foreach (double degrees in hold.HasFlag(SurfaceHold.Flat) ? [0] : StartAngles)
        {
            var start = frame.Start with { RulingAngle = degrees * Math.PI / 180, Bend = new double[BendTerms + 1] };
            var (model, cost) = FitAlone(frame, start, use, hold);
            starts?.Add((degrees, Math.Sqrt(cost / Math.Max(1, use.Count(u => u)))));
            if (cost < bestCost)
            {
                bestCost = cost;
                best = model;
            }
        }

        return (best!, bestCost);
    }

    private static bool[] Reclassify(SurfaceFrame frame, SurfaceModel model, double threshold)
    {
        var mapping = new SurfaceMapping(model, frame.PageLeft, frame.PageTop, frame.PageRight, frame.PageBottom);
        return [.. frame.Image.Select((p, i) => Distance(mapping.ToPage(p), frame.Page[i]) <= threshold)];
    }

    private static (SurfaceModel Model, double Cost) FitAlone(SurfaceFrame frame, SurfaceModel start, bool[] use, SurfaceHold hold)
    {
        int[] indices = [.. Enumerable.Range(0, frame.Image.Count).Where(i => use[i])];
        bool perspective = start.Projection == SurfaceProjection.Perspective;
        var full = Pack(start).Concat(perspective ? PackCamera(start) : []).ToArray();
        var free = FrameFree(hold).Concat(perspective ? CameraFree(hold) : []).ToArray();
        var steps = FrameSteps(perspective).Concat(perspective ? CameraSteps : []).Where((_, i) => free[i]).ToArray();
        SurfaceModel Model(double[] x)
        {
            var all = Merge(full, free, x, 0);
            return Unpack(start, all.AsSpan(0, FrameParameters), perspective ? all.AsSpan(FrameParameters, 3) : default);
        }

        var result = LevenbergMarquardt.Minimise((x, r) => FrameResiduals(frame, Model(x), indices, r, 0), [.. full.Where((_, i) => free[i])], 2 * indices.Length, steps);
        return (Model(result.Parameters), result.Cost);
    }

    private static SurfaceModel[] FitJointly(IReadOnlyList<SurfaceFrame> frames, SurfaceModel[] models, bool[][] kept, SurfaceHold hold)
    {
        var indices = frames.Select((frame, f) => Enumerable.Range(0, frame.Image.Count).Where(i => kept[f][i]).ToArray()).ToArray();
        bool[] frameFree = FrameFree(hold), cameraFree = CameraFree(hold);
        int perFrame = frameFree.Count(b => b);
        var packs = models.Select(Pack).ToArray();
        var cameras = models.Select(PackCamera).ToArray();
        var camera = new[] { Median(models.Select(m => Math.Log(m.Focal))), Median(models.Select(m => m.K1)), Median(models.Select(m => m.K2)) };
        var x0 = packs.SelectMany(p => p.Where((_, i) => frameFree[i])).Concat(camera.Where((_, i) => cameraFree[i])).ToArray();
        var steps = frames.SelectMany(_ => FrameSteps(true).Where((_, i) => frameFree[i])).Concat(CameraSteps.Where((_, i) => cameraFree[i])).ToArray();
        int cameraAt = perFrame * frames.Count;

        // A held camera parameter keeps each frame's own starting value; a free one is shared.
        SurfaceModel Model(double[] x, int f) => Unpack(models[f], Merge(packs[f], frameFree, x, perFrame * f), Merge(cameras[f], cameraFree, x, cameraAt));
        double Residuals(double[] x, double[] r)
        {
            double cost = 0;
            int offset = 0;
            for (int f = 0; f < frames.Count; f++)
            {
                cost += FrameResiduals(frames[f], Model(x, f), indices[f], r, offset);
                offset += 2 * indices[f].Length;
            }

            return cost;
        }

        var result = LevenbergMarquardt.Minimise(Residuals, x0, indices.Sum(i => 2 * i.Length), steps);
        return [.. frames.Select((_, f) => Model(result.Parameters, f))];
    }

    /// <summary>Which of a frame's packed parameters are fitted: the ruling angle and three bend coefficients, then the pose.</summary>
    private static bool[] FrameFree(SurfaceHold hold)
    {
        bool bend = !hold.HasFlag(SurfaceHold.Flat);
        return [bend, bend, bend, bend, true, true, true, true, true, true];
    }

    /// <summary>Which of the packed camera parameters are fitted: log focal length, k1, k2.</summary>
    private static bool[] CameraFree(SurfaceHold hold) =>
        [!hold.HasFlag(SurfaceHold.Focal), !hold.HasFlag(SurfaceHold.Distortion), !hold.HasFlag(SurfaceHold.Distortion)];

    /// <summary><paramref name="full"/> with its free entries replaced, in order, by <paramref name="x"/> from <paramref name="offset"/>.</summary>
    private static double[] Merge(double[] full, bool[] free, double[] x, int offset)
    {
        var merged = (double[])full.Clone();
        for (int i = 0, k = offset; i < full.Length; i++)
        {
            if (free[i])
            {
                merged[i] = x[k++];
            }
        }

        return merged;
    }

    private static double FrameResiduals(SurfaceFrame frame, SurfaceModel model, int[] indices, double[] r, int offset)
    {
        double cost = 0;
        for (int n = 0; n < indices.Length; n++)
        {
            int i = indices[n];
            var predicted = DevelopableSurface.Project(model, DevelopableSurface.Sheet(model, frame.Page[i]));
            var observed = DevelopableSurface.Undistort(model, frame.Image[i]);
            double rx = model.Scale * (predicted.X - observed.X), ry = model.Scale * (predicted.Y - observed.Y);
            if (double.IsNaN(rx) || double.IsNaN(ry))
            {
                rx = ry = 1e6;
            }

            r[offset + (2 * n)] = rx;
            r[offset + (2 * n) + 1] = ry;
            cost += (rx * rx) + (ry * ry);
        }

        return cost;
    }

    private static double[] Pack(SurfaceModel m) => m.Projection == SurfaceProjection.Perspective
        ? [m.RulingAngle, m.Bend[1], m.Bend[2], m.Bend[3], m.RotationX, m.RotationY, m.RotationZ, m.TranslationX, m.TranslationY, m.TranslationZ]
        : [m.RulingAngle, m.Bend[1], m.Bend[2], m.Bend[3], m.RotationX, m.RotationY, m.RotationZ, Math.Log(m.Focal), m.TranslationX, m.TranslationY];

    private static double[] PackCamera(SurfaceModel m) => [Math.Log(m.Focal), m.K1, m.K2];

    private static double[] FrameSteps(bool perspective) => perspective
        ? [1e-6, 1e-6, 1e-6, 1e-6, 1e-7, 1e-7, 1e-7, 1e-4, 1e-4, 1e-4]
        : [1e-6, 1e-6, 1e-6, 1e-6, 1e-7, 1e-7, 1e-7, 1e-7, 1e-7, 1e-7];

    private static readonly double[] CameraSteps = [1e-7, 1e-7, 1e-7];

    private static SurfaceModel Unpack(SurfaceModel template, ReadOnlySpan<double> x, ReadOnlySpan<double> camera)
    {
        double[] bend = [0, x[1], x[2], x[3]];
        if (template.Projection == SurfaceProjection.Perspective)
        {
            return template with
            {
                RulingAngle = x[0], Bend = bend, RotationX = x[4], RotationY = x[5], RotationZ = x[6],
                TranslationX = x[7], TranslationY = x[8], TranslationZ = x[9],
                Focal = camera.Length == 3 ? Math.Exp(camera[0]) : template.Focal,
                K1 = camera.Length == 3 ? camera[1] : template.K1,
                K2 = camera.Length == 3 ? camera[2] : template.K2,
            };
        }

        return template with
        {
            RulingAngle = x[0], Bend = bend, RotationX = x[4], RotationY = x[5], RotationZ = x[6],
            Focal = Math.Exp(x[7]), TranslationX = x[8], TranslationY = x[9],
        };
    }

    private static double[,] Orthonormalise(double[,] r)
    {
        var m = (double[,])r.Clone();
        for (int iteration = 0; iteration < 50; iteration++)
        {
            var inverseTransposed = InverseTranspose(m);
            if (inverseTransposed is null)
            {
                break;
            }

            double change = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    double next = (m[i, j] + inverseTransposed[i, j]) / 2;
                    change += Math.Abs(next - m[i, j]);
                    m[i, j] = next;
                }
            }

            if (change < 1e-14)
            {
                break;
            }
        }

        return m;
    }

    private static double[,]? InverseTranspose(double[,] m)
    {
        double a = m[0, 0], b = m[0, 1], c = m[0, 2], d = m[1, 0], e = m[1, 1], f = m[1, 2], g = m[2, 0], h = m[2, 1], i = m[2, 2];
        double det = (a * ((e * i) - (f * h))) - (b * ((d * i) - (f * g))) + (c * ((d * h) - (e * g)));
        if (Math.Abs(det) < 1e-300)
        {
            return null;
        }

        // The inverse transpose is the cofactor matrix divided by the determinant.
        return new double[,]
        {
            { ((e * i) - (f * h)) / det, -((d * i) - (f * g)) / det, ((d * h) - (e * g)) / det },
            { -((b * i) - (c * h)) / det, ((a * i) - (c * g)) / det, -((a * h) - (b * g)) / det },
            { ((b * f) - (c * e)) / det, -((a * f) - (c * d)) / det, ((a * e) - (b * d)) / det },
        };
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToList();
        return sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[(sorted.Count / 2) - 1] + sorted[sorted.Count / 2]) / 2;
    }

    private static double Norm(double[] v) => Math.Sqrt(v.Sum(x => x * x));

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
