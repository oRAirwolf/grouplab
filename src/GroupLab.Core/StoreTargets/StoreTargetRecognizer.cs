using GroupLab.Core.Imaging;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// One product tried against one picture: how many of its features agree with the fit, the fit from target inches to the picture's own
/// pixels, and how well the picture, read through that fit, has the product's colour layout.
/// </summary>
public sealed record StoreTargetCandidate(StoreTarget Target, int Inliers, Homography? ToImage, double Layout)
{
    /// <summary>The fit in the reduced picture's pixels, where recognition works.</summary>
    internal double[]? Reduced { get; init; }

    /// <summary>
    /// The colour layout's correlation again with the fifth of the cells that disagree most set aside, as the trial measured it: shot holes,
    /// their halos and repair pasters change a few cells a lot.
    /// </summary>
    public double Trimmed { get; init; }
}

/// <summary>
/// Store-bought target recognition, NOTES-FROM-PLANNING.md entry 340 from the trial of entry 332 (docs/notes/fingerprint-trial.md), moved
/// from the command line's trial into Core so the desktop and the phone run the same method with the same thresholds.
/// <para>
/// For each fingerprint: two starts (matches that pass the ratio test, and the three nearest matches of each feature for the patterns a
/// target repeats), a robust homography from target inches to picture pixels, every fingerprint feature looked for where that fit puts it,
/// and the fit made again from what was found. <b>The decision</b>, the trial's: of the products with at least
/// <see cref="LeastInliers"/> features agreeing with their fit, the one whose colour layout, read through the fit, correlates best with the
/// picture, claimed only when that correlation is at least <see cref="LeastLayout"/> and ahead of the next by <see cref="Margin"/>.
/// </para>
/// <para>
/// <b>Families</b>, entry 340 section 2: where the best is a member of a family, the same artwork at different sizes, and the picture cannot
/// tell the sizes apart (<see cref="Decide"/>), the person is asked which it is (<see cref="FamilyQuestion"/>) rather than a size being
/// claimed or nothing.
/// </para>
/// </summary>
public static class StoreTargetRecognizer
{
    /// <summary>The trial's floor on features agreeing with a product's fit.</summary>
    public const int LeastInliers = 25;

    /// <summary>The trial's floor on the colour layout's correlation: above every wrong match the trial saw (0.81).</summary>
    public const double LeastLayout = 0.85;

    /// <summary>How far ahead of the next product the best must be for the picture to tell them apart.</summary>
    public const double Margin = 0.1;

    /// <summary>
    /// A family member's fit carried by at least this many features is taken as telling the sizes apart even below
    /// <see cref="LeastLayout"/> plus <see cref="Margin"/>: shot holes and pasters pull a whole target's layout down to 0.86 on the trial's
    /// pictures, while every crop that claimed the wrong size was carried by 45 to 104 features.
    /// </summary>
    public const int ClearFeatures = 150;

    /// <summary>The ratio test: a match is kept when its nearest rival is further by this factor.</summary>
    private const double Ratio = 0.8;

    /// <summary>The largest descriptor distance, bits, a feature looked for along a fit may be from the fingerprint's.</summary>
    private const int GuidedDistance = 64;

    /// <summary>The layout is read with the picture brought to eight points an inch of the target, smoothed as the fingerprint was.</summary>
    private const double LayoutDpi = 8, LayoutSigma = 1.2;

    /// <summary>
    /// The picture at <paramref name="path"/> against every product in <paramref name="targets"/> (the whole library by default): what it
    /// is, or which family members it could be, or nothing. Null where the file is not a picture.
    /// </summary>
    public static StoreTargetRecognition? Recognize(string path, IFingerprintBackend backend, IReadOnlyList<StoreTarget>? targets = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(backend);
        return backend.Describe(path) is { } picture ? Recognize(picture, backend, targets) : null;
    }

    /// <summary>A described picture against every product: the trial's decision over each product's candidate.</summary>
    public static StoreTargetRecognition Recognize(PictureFeatures picture, IFingerprintBackend backend, IReadOnlyList<StoreTarget>? targets = null)
    {
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(backend);
        var candidates = (targets ?? StoreTargetLibrary.All).Select(t => Try(t, picture, backend)).ToList();

        // A family's members are one artwork at several sizes. Each member is also looked for where each other member's fit puts it, as a
        // third start, so the decision compares the sizes on the same footing and asks when the picture cannot tell them apart.
        var own = candidates.ToList();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Target.Family is not { } family)
            {
                continue;
            }

            foreach (var other in own.Where(o => !ReferenceEquals(o, own[i]) && o.Target.Family == family && o.Reduced is not null))
            {
                if (Hypothesis(candidates[i].Target, other, picture, backend) is { } tried && Better(tried, candidates[i]))
                {
                    candidates[i] = tried;
                }
            }
        }

        return Decide(candidates);
    }

    /// <summary>
    /// The decision over every product's candidate, entry 340 sections 1 and 2: one product named, a family to ask about, or nothing.
    /// <para>
    /// Outside a family, the trial's rule as it was. For a family member, the picture tells the sizes apart only when no other member is
    /// within <see cref="Margin"/> of it and its fit is a strong one: it clears the floor by the margin too, at <see cref="LeastLayout"/>
    /// plus <see cref="Margin"/>, or it is carried by <see cref="ClearFeatures"/> features. Otherwise the person is asked, with every member
    /// of the family, each at its own fit where it has a close one and otherwise where the best fit puts it.
    /// </para>
    /// <para>
    /// Measured on 2026-10-01 with the shipped fingerprints: 300 square crops cut from the two blanks at 45 to 100 pixels an inch, the bull
    /// off centre by up to 0.3 of the sheet, and the trial's 200 pictures. By the trial's rule alone, 4 crops were claimed as the other size,
    /// at a layout agreement of 0.87 to 0.90 with 45 to 104 features. With this rule none was, 36 crops were asked about, and of the trial's
    /// pictures 120 phone views and 42 scans were named as before with 2 asked about; 130 real photographs and scans of other targets were
    /// claimed as none.
    /// </para>
    /// </summary>
    public static StoreTargetRecognition Decide(IReadOnlyList<StoreTargetCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var able = candidates.Where(c => c.Inliers >= LeastInliers && c.ToImage is not null).OrderByDescending(c => c.Layout).ToList();
        if (able.Count == 0 || able[0].Layout < LeastLayout)
        {
            return new StoreTargetRecognition(null, [], candidates);
        }

        var best = able[0];
        var close = able.Where(c => best.Layout - c.Layout < Margin).ToList();
        if (best.Target.Family is not { } family)
        {
            return new StoreTargetRecognition(close.Count == 1 ? StoreTargetMatch.Of(best) : null, [], candidates);
        }

        // Within the margin of a product of another family, or of none: not claimed, as the trial decided.
        if (close.Any(c => c.Target.Family != family))
        {
            return new StoreTargetRecognition(null, [], candidates);
        }

        if (close.Count == 1 && (best.Layout >= LeastLayout + Margin || best.Inliers >= ClearFeatures))
        {
            return new StoreTargetRecognition(StoreTargetMatch.Of(best), [], candidates);
        }

        var members = StoreTargetLibrary.Members(family).Select(m => close.FirstOrDefault(c => c.Target.Id == m.Id) is { } fitted
            ? StoreTargetMatch.Of(fitted)
            : new StoreTargetMatch(m, Homography.Compose(new Homography(Between(m, best.Target)), best.ToImage!), 0, 0)).ToList();
        return new StoreTargetRecognition(null, members, candidates);
    }

    /// <summary>
    /// How one family member's printing lies on another's: <paramref name="target"/>'s inches to <paramref name="sibling"/>'s, as a scale by
    /// their printed widths about their bulls. The Shoot-N-C bullseye's black disc measures 5.88 in across on the 6 in scan and 7.85 in on
    /// the 8 in, 1.334 times larger, the ratio of the printed widths.
    /// </summary>
    public static double[] Between(StoreTarget target, StoreTarget sibling)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sibling);
        TargetFingerprint fp = target.Fingerprint, other = sibling.Fingerprint;
        static PointD Anchor(TargetFingerprint f) => f.Bulls.Count > 0 ? f.Bulls[0] : new PointD(f.Layout.X0 + (f.Layout.Width / 2), f.Layout.Y0 + (f.Layout.Height / 2));
        double k = other.Layout.Width / fp.Layout.Width;
        PointD a = Anchor(other), b = Anchor(fp);
        return [k, 0, a.X - (k * b.X), 0, k, a.Y - (k * b.Y), 0, 0, 1];
    }

    /// <summary>
    /// Whether a fit found from a sibling's is better evidence for its product than the product's own: one that passes the floor on
    /// features where the other does not, or with both passing, the one whose colour layout agrees better, which is what the decision
    /// compares the sizes by.
    /// </summary>
    private static bool Better(StoreTargetCandidate tried, StoreTargetCandidate own)
    {
        bool triedAble = tried.Inliers >= LeastInliers, ownAble = own.Inliers >= LeastInliers && own.ToImage is not null;
        return triedAble && (!ownAble || tried.Layout > own.Layout);
    }

    /// <summary>
    /// <paramref name="target"/> looked for where <paramref name="sibling"/>'s fit puts it, through <see cref="Between"/>: every feature
    /// looked for along that, and the fit made again.
    /// </summary>
    private static StoreTargetCandidate? Hypothesis(StoreTarget target, StoreTargetCandidate sibling, PictureFeatures picture, IFingerprintBackend backend)
    {
        var fp = target.Fingerprint;
        double[] start = Multiply(sibling.Reduced!, Between(target, sibling.Target));
        int w = picture.Lab.Width, h = picture.Lab.Height;
        double threshold = 0.004 * Math.Max(w, h);
        if (!Plausible(start, fp, w, h) || Guided(fp, picture, new Grid(picture.Keys, Math.Max(4, threshold * 2)), start, threshold, backend) is not { } found)
        {
            return null;
        }

        return Candidate(target, found, picture, backend);
    }

    private static double[] Multiply(double[] a, double[] b)
    {
        var r = new double[9];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                r[(i * 3) + j] = (a[i * 3] * b[j]) + (a[(i * 3) + 1] * b[3 + j]) + (a[(i * 3) + 2] * b[6 + j]);
            }
        }

        return r;
    }

    /// <summary>
    /// One fingerprint against one picture: ratio-tested matches and close matches as two starts, each fitted robustly, then every
    /// fingerprint feature looked for near where that fit puts it and the fit made again; the start that agrees with more features is kept.
    /// </summary>
    public static StoreTargetCandidate Try(StoreTarget target, PictureFeatures picture, IFingerprintBackend backend)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(backend);
        var fp = target.Fingerprint;
        var none = new StoreTargetCandidate(target, 0, null, 0);
        if (picture.Keys.Count < 8)
        {
            return none;
        }

        var knn = backend.Nearest(picture, fp, 3);
        var good = knn.Where(m => m.Length >= 2 && m[0].Distance < Ratio * m[1].Distance).Select(m => m[0]).ToList();
        int closeDistance = GuidedDistance * 3 / 4;
        var many = knn.SelectMany(m => m).Where(m => m.Distance <= closeDistance).ToList();
        int w = picture.Lab.Width, h = picture.Lab.Height;
        double threshold = 0.004 * Math.Max(w, h);
        var grid = new Grid(picture.Keys, Math.Max(4, threshold * 2));

        (int Inliers, double[] H)? best = null;
        foreach (var (seed, magsac) in new[] { (good, false), (many, true) })
        {
            if (seed.Count < 8)
            {
                continue;
            }

            var fitted = Fit(backend, [.. seed.Select(m => fp.Points[m.Fingerprint])], [.. seed.Select(m => picture.Keys[m.Picture])], threshold, magsac);
            if (fitted is not { } first || !Plausible(first.H, fp, w, h))
            {
                continue;
            }

            if (Guided(fp, picture, grid, first.H, threshold, backend) is { } found && (best is null || found.Inliers > best.Value.Inliers))
            {
                best = found;
            }
        }

        if (best is not { } chosen)
        {
            return none;
        }

        return Candidate(target, chosen, picture, backend);
    }

    /// <summary>A fit scored by its colour layout, and taken from the reduced picture's pixels to the picture's own, where a marking is made.</summary>
    private static StoreTargetCandidate Candidate(StoreTarget target, (int Inliers, double[] H) fit, PictureFeatures picture, IFingerprintBackend backend)
    {
        var (layout, trimmed) = LayoutScore(target.Fingerprint.Layout, fit.H, picture.Lab, backend);
        var toImage = Homography.Compose(new Homography(fit.H), new Homography([1 / picture.Reduction, 0, 0, 0, 1 / picture.Reduction, 0, 0, 0, 1]));
        return new StoreTargetCandidate(target, fit.Inliers, toImage, layout) { Reduced = fit.H, Trimmed = trimmed };
    }

    private static (int Inliers, double[] H)? Fit(IFingerprintBackend backend, IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, double threshold, bool magsac)
    {
        if (backend.FitHomography(source, destination, threshold, magsac) is not { } fit)
        {
            return null;
        }

        int inliers = fit.Inliers.Count(i => i);
        return inliers < 8 ? null : (inliers, fit.H);
    }

    /// <summary>The picture's features in cells, so those near a point are found without looking at all of them.</summary>
    private sealed class Grid
    {
        private readonly Dictionary<(int, int), List<int>> cells = [];

        public Grid(IReadOnlyList<PointD> keys, double cell)
        {
            Cell = cell;
            for (int i = 0; i < keys.Count; i++)
            {
                var at = ((int)(keys[i].X / cell), (int)(keys[i].Y / cell));
                if (!cells.TryGetValue(at, out var list))
                {
                    cells[at] = list = [];
                }

                list.Add(i);
            }
        }

        public double Cell { get; }

        public IEnumerable<int> Near(PointD p)
        {
            int gx = (int)(p.X / Cell), gy = (int)(p.Y / Cell);
            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (cells.TryGetValue((gx + ox, gy + oy), out var list))
                    {
                        foreach (int i in list)
                        {
                            yield return i;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Each fingerprint feature looked for near where a first fit puts it, and the homography fitted again to what was found, with a
    /// tighter threshold. Null where too little was found or the new fit is not one a camera or scanner could make.
    /// </summary>
    private static (int Inliers, double[] H)? Guided(TargetFingerprint fp, PictureFeatures picture, Grid grid, double[] hm, double threshold, IFingerprintBackend backend)
    {
        int w = picture.Lab.Width, h = picture.Lab.Height, cols = fp.DescriptorBytes;
        var source = new List<PointD>();
        var destination = new List<PointD>();
        for (int j = 0; j < fp.Points.Count; j++)
        {
            var p = Apply(hm, fp.Points[j]);
            if (p.X < 0 || p.Y < 0 || p.X >= w || p.Y >= h)
            {
                continue;
            }

            int bestIdx = -1, bestDist = GuidedDistance;
            foreach (int i in grid.Near(p))
            {
                double dx = picture.Keys[i].X - p.X, dy = picture.Keys[i].Y - p.Y;
                if ((dx * dx) + (dy * dy) > threshold * threshold * 4)
                {
                    continue;
                }

                int dist = Hamming(picture.Descriptors, i * cols, fp.Descriptors, j * cols, cols);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
            {
                source.Add(fp.Points[j]);
                destination.Add(picture.Keys[bestIdx]);
            }
        }

        if (source.Count < 8)
        {
            return null;
        }

        var refined = Fit(backend, source, destination, threshold * 0.6, magsac: false);
        return refined is { } r && Plausible(r.H, fp, w, h) ? r : null;
    }

    /// <summary>A homography a camera or scanner could have made: orientation kept, a sensible scale, not stretched more than a 60 degree tilt.</summary>
    internal static bool Plausible(double[] h, TargetFingerprint fp, int w, int ht)
    {
        var c = Apply(h, fp.Centre);
        var ex = Apply(h, new PointD(fp.Centre.X + 0.5, fp.Centre.Y));
        var ey = Apply(h, new PointD(fp.Centre.X, fp.Centre.Y + 0.5));
        double ax = (ex.X - c.X) * 2, ay = (ex.Y - c.Y) * 2, bx = (ey.X - c.X) * 2, by = (ey.Y - c.Y) * 2;
        double det = (ax * by) - (ay * bx);
        if (det <= 0)
        {
            return false;
        }

        double scale = Math.Sqrt(det);
        double la = Math.Sqrt((ax * ax) + (ay * ay)), lb = Math.Sqrt((bx * bx) + (by * by));
        double stretch = Math.Max(la, lb) / Math.Min(la, lb);
        double perspective = Math.Abs(h[6] / h[8]) + Math.Abs(h[7] / h[8]);
        return scale > 3 && scale < Math.Max(w, ht) && stretch < 2 && perspective < 0.5 && !double.IsNaN(c.X);
    }

    /// <summary>
    /// How well the picture, read through the fit, has the fingerprint's colour layout: the correlation of each Lab channel over the cells
    /// in view, weighted by how much that channel varies on the target. One for a perfect match, near zero for no relation.
    /// </summary>
    internal static (double Full, double Trimmed) LayoutScore(ColourLayout layout, double[] h, LabImage lab, IFingerprintBackend backend)
    {
        double cx = layout.X0 + (layout.Cols * ColourLayout.Cell / 2), cy = layout.Y0 + (layout.Rows * ColourLayout.Cell / 2);
        var c0 = Apply(h, new PointD(cx, cy));
        var c1 = Apply(h, new PointD(cx + 1, cy));
        var c2 = Apply(h, new PointD(cx, cy + 1));
        double scale = Math.Sqrt(Math.Abs(((c1.X - c0.X) * (c2.Y - c0.Y)) - ((c1.Y - c0.Y) * (c2.X - c0.X))));
        double f = Math.Min(1, LayoutDpi / Math.Max(1e-6, scale));
        var small = backend.Smooth(lab, f, LayoutSigma * Math.Min(1, scale * f / LayoutDpi));
        var r = new List<double>[3];
        var q = new List<double>[3];
        for (int k = 0; k < 3; k++)
        {
            r[k] = [];
            q[k] = [];
        }

        for (int row = 0; row < layout.Rows; row++)
        {
            for (int col = 0; col < layout.Cols; col++)
            {
                var p = Apply(h, new PointD(layout.X0 + ((col + 0.5) * ColourLayout.Cell), layout.Y0 + ((row + 0.5) * ColourLayout.Cell)));
                if (p.X < 2 || p.Y < 2 || p.X > lab.Width - 2 || p.Y > lab.Height - 2)
                {
                    continue;
                }

                var v = Sample(small, p.X * f, p.Y * f);
                int i = ((row * layout.Cols) + col) * 3;
                for (int k = 0; k < 3; k++)
                {
                    r[k].Add(layout.Lab[i + k]);
                    q[k].Add(v[k]);
                }
            }
        }

        if (r[0].Count < 12)
        {
            return (0, 0);
        }

        var keep = Enumerable.Range(0, r[0].Count).ToList();
        double full = Correlation(r, q, keep, out var residual);
        int drop = keep.Count / 5;
        keep = [.. keep.OrderBy(i => residual[i]).Take(keep.Count - drop)];
        return (full, Correlation(r, q, keep, out _));
    }

    private static double Correlation(List<double>[] r, List<double>[] q, List<int> keep, out double[] residual)
    {
        residual = new double[r[0].Count];
        double sum = 0, weights = 0;
        for (int c = 0; c < 3; c++)
        {
            double mr = keep.Average(i => r[c][i]), mq = keep.Average(i => q[c][i]);
            double cov = 0, vr = 0, vq = 0;
            foreach (int i in keep)
            {
                cov += (r[c][i] - mr) * (q[c][i] - mq);
                vr += (r[c][i] - mr) * (r[c][i] - mr);
                vq += (q[c][i] - mq) * (q[c][i] - mq);
            }

            double weight = Math.Sqrt(vr / keep.Count);
            sum += vq <= 1e-9 || vr <= 1e-9 ? 0 : weight * cov / Math.Sqrt(vr * vq);
            weights += weight;
            double sr = Math.Sqrt(vr / keep.Count), sq = Math.Sqrt(vq / keep.Count);
            for (int i = 0; i < residual.Length; i++)
            {
                if (sr > 1e-9 && sq > 1e-9)
                {
                    residual[i] += weight * Math.Abs(((q[c][i] - mq) / sq) - ((r[c][i] - mr) / sr));
                }
            }
        }

        return weights <= 0 ? 0 : sum / weights;
    }

    /// <summary>The Lab picture's colour at a point, bilinear, as OpenCV's pixel centres are.</summary>
    public static double[] Sample(LabImage lab, double x, double y)
    {
        int x0 = Math.Clamp((int)Math.Floor(x - 0.5), 0, lab.Width - 1), y0 = Math.Clamp((int)Math.Floor(y - 0.5), 0, lab.Height - 1);
        int x1 = Math.Min(x0 + 1, lab.Width - 1), y1 = Math.Min(y0 + 1, lab.Height - 1);
        double fx = Math.Clamp(x - 0.5 - x0, 0, 1), fy = Math.Clamp(y - 0.5 - y0, 0, 1);
        var result = new double[3];
        for (int k = 0; k < 3; k++)
        {
            double a = lab.Pixels[(((y0 * lab.Width) + x0) * 3) + k], b = lab.Pixels[(((y0 * lab.Width) + x1) * 3) + k];
            double c = lab.Pixels[(((y1 * lab.Width) + x0) * 3) + k], d = lab.Pixels[(((y1 * lab.Width) + x1) * 3) + k];
            result[k] = (a * (1 - fx) * (1 - fy)) + (b * fx * (1 - fy)) + (c * (1 - fx) * fy) + (d * fx * fy);
        }

        return result;
    }

    internal static PointD Apply(double[] h, PointD p)
    {
        double w = (h[6] * p.X) + (h[7] * p.Y) + h[8];
        return new PointD(((h[0] * p.X) + (h[1] * p.Y) + h[2]) / w, ((h[3] * p.X) + (h[4] * p.Y) + h[5]) / w);
    }

    private static int Hamming(byte[] a, int ai, byte[] b, int bi, int n)
    {
        int d = 0;
        for (int k = 0; k < n; k++)
        {
            d += System.Numerics.BitOperations.PopCount((uint)(a[ai + k] ^ b[bi + k]));
        }

        return d;
    }
}
