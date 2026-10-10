using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 324 section 1: a photograph's lens fit with a smooth correction across the sheet on top of it, for a
/// margin lifted off the sheet's plane. Entry 322 found the far column of the 9 and 15 degree pictures of the 2026-09-29 sitting read 10 to
/// 19 pixels from where the lens fit put it, every marker on it decoded, each read about a tenth wider than a flat sheet allows: the margin
/// had lifted toward the camera, which no lens and no plane can follow, so the lens fit rightly left those markers out and the holes beside
/// them measured up to 0.08 in off.
/// <para>
/// The lens fit stays what it was, fitted to the corners it kept. What it leaves at every corner, the page position read less the page
/// position it predicts, is fitted by a smoothing thin-plate spline over the page (the spline <see cref="MarkerMesh"/> passes through
/// every corner with, here smoothed so a corner's own noise is not followed), and the page position of any pixel is the lens fit's plus
/// that correction there. Its smoothing is chosen by predicting each marker from a correction fitted without it. Past the markers the
/// correction fades to nothing over <see cref="FadeDmm"/>, so the printed panel below them and the paper's edges are read as the lens fit
/// reads them: on the 2 degree picture a correction carried on past the last row moved the panel's border enough to raise a false mark.
/// </para>
/// <para>
/// It is taken only where it is a bend and not noise or a misread: the lens fit must have left out a marker at least
/// <see cref="LiftedFromDmm"/> from where it put it, and the correction fitted without that marker must put it near where it was read,
/// as a lifted margin's neighbours, lifted with it, do and a misread's or a noisy corner's do not; the corners the lens fit kept must fit no
/// worse than they did; the correction must stay within <see cref="MostDmm"/> over the markers; and more corners must be kept than the
/// lens fit kept. A flat sheet's lens fit keeps every corner, or leaves a few out by a whisker, and the correction is never fitted.
/// </para>
/// </summary>
public sealed class BentSheetMapping : IPageMapping
{
    /// <summary>A corner a correction leaves farther than this from where it was read, dmm, is taken as misread and left out of it.</summary>
    public const double MisreadBeyondDmm = 3 * PageRegistration.RansacThreshold;

    /// <summary>How far a marker the lens fit left out may always be from where a correction fitted without it predicts it, dmm, as a root mean square over its corners.</summary>
    public const double AgreeWithinDmm = PageRegistration.RansacThreshold;

    /// <summary>
    /// The share of a left-out marker's distance from the lens fit that a correction fitted without it may leave, where that is more than
    /// <see cref="AgreeWithinDmm"/>: the end of a lifted column is predicted by reaching past its neighbours, and lands within half its lift.
    /// </summary>
    public const double ExplainedShare = 0.5;

    /// <summary>
    /// How far from the lens fit a left-out marker must be, dmm, as a root mean square over its corners, for the sheet to be taken as bent:
    /// 0.04 in. The 9 and 15 degree pictures' far columns are 17 to 30 dmm off. The 1 and 2 degree pictures' are 6 to 10, and there the
    /// correction moved the holes by less than the hole finder's own spread, one hard hole's error up as often as down.
    /// </summary>
    public const double LiftedFromDmm = 4 * PageRegistration.RansacThreshold;

    /// <summary>The largest correction taken, dmm: 0.2 in, more than half again the far column's lift on the 15 degree picture.</summary>
    public const double MostDmm = 50.8;

    /// <summary>How far past the markers the correction fades to nothing, dmm: a quarter of an inch.</summary>
    public const double FadeDmm = 63.5;

    /// <summary>The smoothing tried, in the spline's normalised units; the one that predicts each marker best from the rest is taken.</summary>
    private static readonly double[] Smoothings = [1e-3, 1e-2, 1e-1, 1];

    private readonly MarkerMesh.Spline field;

    private readonly (double Left, double Top, double Right, double Bottom) span;

    private BentSheetMapping(RadialHomographyMapping lens, MarkerMesh.Spline field, IReadOnlyList<PointD> points, IReadOnlyList<PointD> corrections, double smoothing)
    {
        Lens = lens;
        this.field = field;
        Points = points;
        Corrections = corrections;
        Smoothing = smoothing;
        span = (points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
    }

    /// <summary>The lens fit the correction sits on.</summary>
    public RadialHomographyMapping Lens { get; }

    /// <summary>Where the lens fit put each corner the correction was fitted to, page dmm.</summary>
    public IReadOnlyList<PointD> Points { get; }

    /// <summary>The correction each of those corners asked for, page dmm: where it was read less where the lens fit put it.</summary>
    public IReadOnlyList<PointD> Corrections { get; }

    /// <summary>The spline's smoothing, kept so a saved session rebuilds the same correction.</summary>
    public double Smoothing { get; }

    public string Model => "homography with radial distortion and a bent sheet";

    /// <summary>The correction at a page position the lens fit gives, dmm, fading to nothing past the markers.</summary>
    public PointD Correction(PointD lensPage)
    {
        var (left, top, right, bottom) = span;
        double ox = Math.Max(0, Math.Max(left - lensPage.X, lensPage.X - right)), oy = Math.Max(0, Math.Max(top - lensPage.Y, lensPage.Y - bottom));
        double w = 1 - (Math.Sqrt((ox * ox) + (oy * oy)) / FadeDmm);
        if (w <= 0)
        {
            return default;
        }

        var c = field.At(lensPage);
        return w >= 1 ? c : new PointD(c.X * w, c.Y * w);
    }

    public PointD ToPage(PointD image)
    {
        var q = Lens.ToPage(image);
        var d = Correction(q);
        return new PointD(q.X + d.X, q.Y + d.Y);
    }

    public PointD ToImage(PointD page)
    {
        // The correction is small and smooth, so the lens fit's page position under it is found by repeated substitution.
        var q = page;
        for (int i = 0; i < 20; i++)
        {
            var d = Correction(q);
            var next = new PointD(page.X - d.X, page.Y - d.Y);
            bool converged = Math.Abs(next.X - q.X) + Math.Abs(next.Y - q.Y) < 1e-9;
            q = next;
            if (converged)
            {
                break;
            }
        }

        return Lens.ToImage(q);
    }

    public (double XX, double XY, double YX, double YY) Jacobian(PointD image)
    {
        const double h = 0.5;
        var px = ToPage(new PointD(image.X + h, image.Y));
        var mx = ToPage(new PointD(image.X - h, image.Y));
        var py = ToPage(new PointD(image.X, image.Y + h));
        var my = ToPage(new PointD(image.X, image.Y - h));
        return ((px.X - mx.X) / (2 * h), (py.X - my.X) / (2 * h), (px.Y - mx.Y) / (2 * h), (py.Y - my.Y) / (2 * h));
    }

    /// <summary>The correction a saved session kept, rebuilt from its corners and smoothing: the same fit, so the same mapping. Null where they do not pair up.</summary>
    public static BentSheetMapping? Rebuild(RadialHomographyMapping lens, IReadOnlyList<PointD> points, IReadOnlyList<PointD> corrections, double smoothing)
    {
        ArgumentNullException.ThrowIfNull(lens);
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(corrections);
        if (points.Count != corrections.Count || points.Count < 4)
        {
            return null;
        }

        try
        {
            return new BentSheetMapping(lens, MarkerMesh.Spline.Fit([.. points], [.. corrections], smoothing), [.. points], [.. corrections], smoothing);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// The bent sheet over <paramref name="lens"/>, or null where the corners do not support one. The corners are image pixels and page dmm,
    /// four to a marker in order; <paramref name="kept"/> is which of them the lens fit kept.
    /// </summary>
    public static BentSheetFit? Fit(RadialHomographyMapping lens, IReadOnlyList<PointD> image, IReadOnlyList<PointD> page, IReadOnlyList<bool> kept)
    {
        ArgumentNullException.ThrowIfNull(lens);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(kept);
        int n = image.Count;
        if (n != page.Count || n != kept.Count || n % 4 != 0 || kept.All(k => k))
        {
            return null;
        }

        var q = image.Select(lens.ToPage).ToArray();
        var t = q.Select((p, i) => new PointD(page[i].X - p.X, page[i].Y - p.Y)).ToArray();
        double LensRms(int m) => Math.Sqrt(Enumerable.Range(4 * m, 4).Average(i => Squared(t[i])));

        // A corner the lens fit puts beyond the largest bend taken is a misread, not a lift, and a marker with one is left out whole.
        var use = Enumerable.Range(0, n).Where(i => Length(t[i]) <= MostDmm).ToList();
        use = [.. use.GroupBy(i => i / 4).Where(g => g.Count() == 4).SelectMany(g => g)];
        bool AnyLifted() => use.Where(i => !kept[i]).Select(i => i / 4).Distinct().Any(m => LensRms(m) >= LiftedFromDmm);
        if (use.Count < 48 || !AnyLifted())
        {
            return null;
        }

        try
        {
            // Smoothing chosen by each marker predicted from the rest, then a marker the correction cannot reach is left out of it.
            double smoothing = Smoothings.MinBy(l => LeaveOneOut(q, t, use, l).Rms);
            var first = MarkerMesh.Spline.Fit([.. use.Select(i => q[i])], [.. use.Select(i => t[i])], smoothing);
            var misread = use.Where(i => Length(Minus(t[i], first.Exactly(q[i]))) > MisreadBeyondDmm).Select(i => i / 4).ToHashSet();
            use = [.. use.Where(i => !misread.Contains(i / 4))];
            if (use.Count < 48 || !AnyLifted())
            {
                return null;
            }

            // A marker the lens fit left out joins the correction only where the correction fitted without it puts it near where it was
            // read. A lifted margin's markers are; a noisy or misread one, its neighbours flat, is not, and stays left out as before.
            var (_, perMarker) = LeaveOneOut(q, t, use, smoothing);
            var leftOut = use.Where(i => !kept[i]).Select(i => i / 4).Distinct().ToList();
            var unexplained = leftOut.Where(m => perMarker[m] > Math.Max(AgreeWithinDmm, ExplainedShare * LensRms(m))).ToHashSet();
            var lifted = leftOut.Where(m => !unexplained.Contains(m) && LensRms(m) >= LiftedFromDmm).ToList();
            if (lifted.Count == 0)
            {
                return null;
            }

            use = [.. use.Where(i => !unexplained.Contains(i / 4))];
            var (leaveOneOut, after) = LeaveOneOut(q, t, use, smoothing);
            var field = MarkerMesh.Spline.Fit([.. use.Select(i => q[i])], [.. use.Select(i => t[i])], smoothing);
            var mapping = new BentSheetMapping(lens, field, [.. use.Select(i => q[i])], [.. use.Select(i => t[i])], smoothing);

            // The corners the lens fit kept must fit no worse than they did.
            var core = Enumerable.Range(0, n).Where(i => kept[i]).ToList();
            double lensCore = Math.Sqrt(core.Average(i => Squared(t[i])));
            double bentCore = Math.Sqrt(core.Average(i => Squared(Minus(page[i], mapping.ToPage(image[i])))));
            if (bentCore > lensCore)
            {
                return null;
            }

            // The correction stays bounded over the span of the markers, not only at them.
            var (left, top, right, bottom) = mapping.span;
            double most = 0;
            for (int a = 0; a <= 16; a++)
            {
                for (int b = 0; b <= 16; b++)
                {
                    most = Math.Max(most, Length(field.At(new PointD(left + ((right - left) * a / 16), top + ((bottom - top) * b / 16)))));
                }
            }

            if (most > MostDmm)
            {
                return null;
            }

            bool[] bentKept = [.. Enumerable.Range(0, n).Select(i => Length(Minus(page[i], mapping.ToPage(image[i]))) <= PageRegistration.RansacThreshold)];
            if (bentKept.Count(k => k) <= kept.Count(k => k))
            {
                return null;
            }

            return new BentSheetFit(mapping, bentKept, leaveOneOut, lifted.Max(m => after[m]), most);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Each marker's corners predicted by a correction fitted without that marker: the root mean square over all, and each marker's own.</summary>
    private static (double Rms, Dictionary<int, double> PerMarker) LeaveOneOut(PointD[] q, PointD[] t, List<int> use, double smoothing)
    {
        // Entry 400: each marker's fit without it is its own, so they are fitted at once, then summed in the markers' order as before, so the
        // same additions in the same order give the same result to the last bit. On a bent photograph this was most of registering it.
        var markers = use.GroupBy(i => i / 4).ToList();
        var owns = new double[markers.Count];
        try
        {
            Parallel.For(0, markers.Count, m =>
            {
                var marker = markers[m];
                var rest = use.Where(i => i / 4 != marker.Key).ToList();
                var without = MarkerMesh.Spline.Fit([.. rest.Select(i => q[i])], [.. rest.Select(i => t[i])], smoothing);
                owns[m] = marker.Sum(i => Squared(Minus(t[i], without.Exactly(q[i]))));
            });
        }
        catch (AggregateException e) when (e.InnerExceptions.Count > 0)
        {
            // What one fit throws is thrown as itself, as the loop this replaces threw it; Fit catches it.
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerExceptions[0]).Throw();
        }

        double sum = 0;
        var perMarker = new Dictionary<int, double>();
        for (int m = 0; m < markers.Count; m++)
        {
            sum += owns[m];
            perMarker[markers[m].Key] = Math.Sqrt(owns[m] / markers[m].Count());
        }

        return (Math.Sqrt(sum / use.Count), perMarker);
    }

    private static PointD Minus(PointD a, PointD b) => new(a.X - b.X, a.Y - b.Y);

    private static double Squared(PointD v) => (v.X * v.X) + (v.Y * v.Y);

    private static double Length(PointD v) => Math.Sqrt(Squared(v));
}

/// <summary>
/// A bent sheet that was taken: the mapping; which corners it keeps within the scan threshold; each marker predicted by a correction fitted
/// without it, as a root mean square over every corner, which is the error between markers a person is told; the worst of those over the
/// lifted markers; and the largest correction over the markers. Distances are dmm.
/// </summary>
public sealed record BentSheetFit(BentSheetMapping Mapping, IReadOnlyList<bool> Kept, double LeaveOneOutRms, double WorstLiftedDmm, double MostDmm);
