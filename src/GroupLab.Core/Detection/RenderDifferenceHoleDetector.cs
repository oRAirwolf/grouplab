using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Detection;

/// <summary>
/// The settings of <see cref="RenderDifferenceHoleDetector"/>. Every length is in inches and every threshold a fraction of
/// the local paper-to-ink range, never a grey level, because docs/DETECTION-PIPELINE.md stage S7 measured one absolute
/// threshold needing 28, 18 and 12 at 600, 93 and 300 DPI on the same targets.
/// <para>
/// The opening is not the survey's. Its 0.032 in radius exists to erase printed artwork, which in a difference has already
/// cancelled, and on synthetic sheets it erased holes whose rim is thinner than the 0.064 in disk around a core near paper
/// level, the misses of both detectors (docs/PHASE1-RESULTS.md M2.2). What is left to remove after differencing is the thin
/// sliver a registration error leaves along every printed edge, so the opening here is sized for that.
/// </para>
/// <para>
/// The split threshold is 1.80, NOTES-FROM-PLANNING.md entry 81 section 1. It was 1.45, set between the elongation of single synthetic
/// holes, 99th percentile 1.26 to 1.37 by case, and that of merged neighbours, 5th percentile 1.51 and 1.58, on the development seeds
/// (docs/PHASE1-RESULTS.md M2.2). Real holes overruled it: at 1.45 a real hole joined to printed or photographed ink is cut in two, which
/// invents a shot nobody can see, and at 1.80 no real hole in the corpus is split (entry 80 section 2). A merged pair left whole is the
/// louder failure, one mark of about twice a hole's area, which the oversize flag reports.
/// </para>
/// </summary>
public sealed record RenderDifferenceOptions(
    double OpenRadiusInches = 0.012,
    double CloseRadiusInches = 0.055,
    double ResidualFraction = 0.15,
    double MinimumDiameterInches = 0.15,
    double MaximumDiameterInches = 0.60,
    double MinimumSolidity = 0.55,
    double MaximumAspect = 2.2,
    double PaperBlockInches = 0.25,
    double MaximumShiftInches = 0.1,
    double MinimumClosure = 0,
    double SplitElongation = 1.80,
    /// <summary>The size a single hole is detected at, inches: the calibre times what holes of it measure, never the bullet diameter itself.</summary>
    double? CalibreInches = null,
    double SplitMinimumHoles = 1.5,
    double OversizeHoles = 1.35,
    double ResidueElongation = 2.2,
    double SmallestHoleInches = 0.16,
    double LargestHoleInches = 0.60,
    int MarksForSheetSize = 12,
    int MarksForTentativeSize = 5);

/// <summary>Where the size of a single hole came from, NOTES-FROM-PLANNING.md entry 82.</summary>
public enum HoleSizeSource
{
    /// <summary>The calibre the person named, times what holes of it measure.</summary>
    Calibre,

    /// <summary>The sheet's own quarter-point round mark, from enough marks to trust.</summary>
    Sheet,

    /// <summary>The sheet's own quarter-point round mark, from too few marks to trust fully: its flags are tentative and it vetoes no split.</summary>
    SheetTentative,

    /// <summary>No size the sheet can give, so only the smallest hole any bullet makes: enough to veto a split, never to flag.</summary>
    Bound,

    /// <summary>The round marks fall into two sizes, so no one size fits the sheet, and the person is asked for the calibre.</summary>
    TwoSizes,
}

/// <summary>
/// The size of a single hole a pass judged marks against, inches, and where it came from. <see cref="VetoInches"/> is the size a split is
/// vetoed against and <see cref="FlagInches"/> the size oversized marks are flagged against, null where the source cannot support it.
/// </summary>
public sealed record HoleSizeReference(HoleSizeSource Source, double VetoInches, double? FlagInches, int RoundMarks, string Description);

/// <summary>
/// A hole found by render-and-difference, image pixels: the intensity-weighted centroid of its residual, its hull diameter,
/// whether it sits on printed ink, its rim closure, the fraction of 360 rays from the centre that meet residual, the ratio of
/// the principal standard deviations of that residual, and whether it is one of two holes split from a single blob.
/// <see cref="InkFraction"/> is how much of the expected printed artwork lies inside the detection's own footprint, the mean ink coverage
/// over its hull: the quantity NOTES-FROM-PLANNING.md entry 86 section 3 asks for, which tells a hole centred on a ring from one beside it
/// where the distance to the nearest edge cannot.
/// <para>
/// <see cref="AreaInches"/> and <see cref="Aspect"/> are the shape, NOTES-FROM-PLANNING.md entry 88 section 1: a mark larger than one hole
/// has three possible causes and the flag names two of them. The third is a bullet that arrived yawed, which makes one oval hole rather than
/// two round ones, and the three separate by shape. The area is the hull's, in square inches at the blob's own scale, and the aspect is its
/// bounding box's long side over its short side. <see cref="Elongation"/> is the same quantity from the residual-weighted second moments,
/// which is orientation-free where the box is not, and <see cref="Solidity"/> is what a waist between two lobes shows in.
/// </para>
/// <para>
/// A split half carries its parent blob's geometry, as it already does for the diameter, because a half has no separate hull.
/// </para>
/// </summary>
public sealed record RenderDifferenceHole(double X, double Y, double HullX, double HullY, double DiameterInches, double Solidity, bool OnInk, double Closure,
    double Elongation = double.NaN, bool PossibleMerge = false, bool Oversized = false, double? CalibreHoles = null, bool SplitVetoed = false,
    double? SizeHoles = null, bool OversizeTentative = false, double InkFraction = 0, double AreaInches = 0, double Aspect = double.NaN);

/// <summary>
/// One render-and-difference pass: the resolution, the measured ink level as a fraction of paper, the resolved residual
/// threshold in grey levels, what was kept and what was refused, the local shift, pixels, each bull's cell was aligned by, and the
/// expected artwork in image pixels after that alignment, which is where the printed rings, numerals and markers are. The marking
/// screen's snap and size check read it to tell printed ink from a hole (NOTES-FROM-PLANNING.md entry 40 section 1).
/// </summary>
public sealed record RenderDifferenceResult(double Dpi, double InkFraction, double Threshold, IReadOnlyList<RenderDifferenceHole> Holes, IReadOnlyList<RejectedBlob> Rejected, IReadOnlyList<PointD> CellShifts, GrayImage? Expected = null, HoleSizeReference? HoleSize = null)
{
    /// <summary>
    /// The candidates an exclusion zone swallowed, NOTES-FROM-PLANNING.md entry 77 section 3 item 1: each passed every size, compactness and
    /// shape filter and was refused only because it lies inside a zone. A zone is blind, so a hole there is dropped from every figure, and this
    /// count is how that shows as a number rather than as nothing.
    /// </summary>
    public IReadOnlyList<RejectedBlob> InsideZones => [.. Rejected.Where(r => r.Zone is not null)];
}

/// <summary>
/// Render-and-difference, stages S5 to S8 of docs/DETECTION-PIPELINE.md and docs/PHASE1-BRIEF.md section 4.2. A GroupLab
/// sheet is registered and its definition says exactly what was printed, so the expected image is rendered through the
/// registration and whatever the observed image has that the expected one does not is what a shot did.
/// <list type="number">
/// <item><b>S5, alignment.</b> A registration is never exact, and every printed edge it misses by leaves a sliver in the
/// difference that a small opening cannot remove: on synthetic sheets a 0.02 in error cost 40 percent of holes and 0.04 in
/// all of them (docs/PHASE1-RESULTS.md M2.2). The printed rings are fiducials in their own right, so each bull's cell of the
/// expected image is aligned to the observed one by phase correlation, up to <see cref="RenderDifferenceOptions.MaximumShiftInches"/>,
/// and the rest of the page by the median of those shifts.</item>
/// <item><b>S5.</b> The definition is rasterised and resampled into image space (<see cref="ExpectedImage"/>). Paper is
/// estimated locally, as the 95th percentile of the observed pixels the render calls paper in blocks of a quarter inch,
/// smoothed and interpolated, never globally. The ink level is measured as the median ratio of observed to local paper
/// where the render calls solid ink.</item>
/// <item><b>S6.</b> Observed and expected are both normalised by local paper, and the residual is the absolute difference
/// as a fraction of the paper-to-ink range: darker than expected where a rim darkens paper, and lighter than expected where
/// a perforation removes printed ink, which is the only signal a hole on a black disc gives.</item>
/// <item><b>S7.</b> The residual is opened with a disk wider than any printed stroke, which also erases the thin slivers a
/// small registration error leaves along every printed edge, thresholded at a fraction of the range, closed, filled, and
/// each blob measured by its convex hull.</item>
/// <item><b>S8.</b> The survey's size, compactness and elongation filters; a candidate centred inside a zone the definition
/// declares printed matter, a marker footprint, a code, the data block or the identifier, is refused with the zone named;
/// and one centred outside every bull's cell is refused, which is the position prior that would have removed every false
/// positive the survey's baselines made. An accepted hole's centre is the residual-weighted centroid over its hull, not the
/// hull's centroid, because a lobed rim puts a hull centroid wherever the lobes fall. Rim closure, S8's added signature, is
/// measured and recorded but not gated: the fraction of 360 rays from that centre meeting thresholded residual within one
/// hull diameter read 0.65 to 0.81 on synthetic arrowheads against holes as low as 0.73, and a real rim is often a C, which
/// the synthesis does not draw (docs/PHASE1-RESULTS.md M2.2).</item>
/// <item><b>S8, merged neighbours.</b> Two holes within a closing radius of each other become one blob, as at bull 20 of
/// <c>300_nm_hand_load</c>, which docs/DETECTION-PIPELINE.md's corpus table requires reported as two shots or flagged, never
/// silently as one. A blob whose residual's principal standard deviations differ by a ratio of at least
/// <see cref="RenderDifferenceOptions.SplitElongation"/>, up to twice the largest single hole's area, is split by weighted
/// two-means into two holes, each flagged as a possible merge. A whole mark with the area of
/// <see cref="RenderDifferenceOptions.OversizeHoles"/> single holes or more is flagged as oversized, because an overlapping pair's
/// residual can be as round as a single hole's and no split finds it; a single hole is the calibre's size, or without one the sheet's
/// 25th percentile whole mark.</item>
/// </list>
/// </summary>
public static class RenderDifferenceHoleDetector
{
    private sealed record Zone(string Name, double Left, double Top, double Right, double Bottom);

    /// <param name="observed">The image, grey, as V or luminance.</param>
    /// <param name="registration">Image pixels to page dmm.</param>
    /// <param name="dpi">The image's resolution at the page, to convert inches to pixels and to rasterise the definition at.</param>
    /// <param name="pageRender">The tile's render at <paramref name="dpi"/>, when the caller already has one.</param>
    public static RenderDifferenceResult Detect(GrayImage observed, TargetDefinition definition, int tile, IPageMapping registration, double dpi, IImagingBackend backend,
        RenderDifferenceOptions? options = null, GrayImage? pageRender = null)
    {
        ArgumentNullException.ThrowIfNull(observed);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(backend);
        options ??= new RenderDifferenceOptions();
        // The expected artwork of a sheet already printed, drawn even when today's validator would refuse to print it: the Phase 0 sheets
        // fail test 26f since entry 13 made it an error, and a sheet on paper is what it is (found by the first end-to-end run, entry 33).
        var pages = SceneBuilder.Build(definition, new RenderOptions(AllowInvalid: true)).Pages;
        if (tile < 0 || tile >= pages.Count)
        {
            throw new InvalidOperationException(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"the definition renders {pages.Count} page(s), so tile {tile} has no expected artwork to difference against"));
        }

        var scene = pages[tile];
        pageRender ??= SceneRasterizer.Rasterize(scene, dpi);
        int width = observed.Width, height = observed.Height;
        var expected = ExpectedImage.Render(pageRender, dpi, registration, width, height);

        // Detection runs inside the registered sheet, NOTES-FROM-PLANNING.md entry 23 section 4 and docs/DETECTION-PIPELINE.md before S5.
        // Beyond the page's edge the observed image is replaced by the sheet's own paper level before any stage reads it, so a dark mat
        // around a photographed sheet can neither lower the local paper estimate nor become a residual, whatever image the caller passed.
        var inside = SheetMask(definition.Page.Width, definition.Page.Height, registration, width, height);
        observed = WithinSheet(observed, expected, inside);

        // S5: local paper, from pixels the render calls paper, and the ink level against it.
        int block = Math.Max(4, (int)Math.Round(options.PaperBlockInches * dpi));
        var paper = PaperField(observed, expected, block);
        double inkFraction = InkFraction(observed, expected, paper, block);

        // S5, alignment: each bull's cell by phase correlation, the rest by the median shift.
        var cells = BullCells.Of(definition);
        var (aligned, shifts) = Align(observed, expected, paper, block, inkFraction, cells, registration, options.MaximumShiftInches * dpi, backend);
        expected = aligned;
        inkFraction = InkFraction(observed, expected, paper, block);

        // S6: the residual as a fraction of the paper-to-ink range, both signs.
        double range = Math.Max(0.05, 1 - inkFraction);
        var residual = new byte[width * height];
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width) + x;
                double p = Math.Max(1, PaperAt(paper, block, width, height, x, y));
                double e = 1 - ((1 - (expected.Pixels[i] / 255.0)) * (1 - inkFraction));
                double n = observed.Pixels[i] / p;
                residual[i] = (byte)Math.Clamp(Math.Round(255 * Math.Abs(e - n) / range), 0, 255);
            }
        });

        // And nothing beyond the page's edge can become a candidate: on the N568 photograph the mat merged into one blob that swallowed every hole.
        for (int i = 0; i < residual.Length; i++)
        {
            if (!inside[i])
            {
                residual[i] = 0;
            }
        }

        // S7: open, threshold, close, fill, hull.
        int open = Math.Max(2, (int)Math.Round(options.OpenRadiusInches * dpi, MidpointRounding.ToEven));
        int close = Math.Max(3, (int)Math.Round(options.CloseRadiusInches * dpi, MidpointRounding.ToEven));
        var opened = backend.Morphology(new GrayImage(width, height, residual), MorphologyOperation.Open, open);
        double threshold = options.ResidualFraction * 255;
        var binary = new byte[width * height];
        for (int i = 0; i < binary.Length; i++)
        {
            binary[i] = opened.Pixels[i] >= threshold ? (byte)1 : (byte)0;
        }

        var closed = backend.Morphology(new GrayImage(width, height, binary), MorphologyOperation.Close, close);

        // S8: filters, declared zones, the position prior, and the weighted centre.
        var zones = Zones(definition, scene, tile);
        // NOTES-FROM-PLANNING.md entry 83 section 2: every size is converted at the blob's own scale. One scale for the whole image read holes
        // on the near side of an oblique photograph up to 30 percent large, which is what the oversize flags on those frames were.
        double largestSquareInches = Math.PI * Math.Pow(options.MaximumDiameterInches / 2, 2);
        var holes = new List<RenderDifferenceHole>();
        var rejected = new List<RejectedBlob>();
        var elongatedBlobs = new List<(ImageBlob Blob, BlobMoments Moments, double AreaInches, double Hx, double Hy, double DiameterInches, double Solidity, double Closure, double? CalibreHoles)>();
        foreach (var blob in backend.FilledBlobs(closed))
        {
            var (area, hx, hy) = Polygon(blob.Hull);
            if (area <= 0)
            {
                continue;
            }

            double diameter = 2 * Math.Sqrt(area / Math.PI), solidity = blob.Area / area;
            double ppi = LocalPixelsPerInch(registration, new PointD(hx, hy));
            double diameterIn = diameter / ppi, areaIn = area / (ppi * ppi);
            bool tooSmall = diameterIn < options.MinimumDiameterInches;
            double aspect = BoxAspect(blob);
            var page = registration.ToPage(new PointD(hx, hy));
            var zone = zones.FirstOrDefault(z => page.X >= z.Left && page.X <= z.Right && page.Y >= z.Top && page.Y <= z.Bottom);
            var moments = tooSmall ? default : Moments(residual, expected, width, blob);
            double? calibreHoles = options.CalibreInches is { } calibre ? areaIn / (Math.PI * Math.Pow(calibre / 2, 2)) : null;
            bool elongated = moments.Elongation >= options.SplitElongation && areaIn <= 2 * largestSquareInches;
            string? shape = tooSmall ? FormattableString.Invariant($"too small, {diameterIn:0.000} in")
                : diameterIn > options.MaximumDiameterInches && !elongated ? FormattableString.Invariant($"too large, {diameterIn:0.000} in")
                : solidity < options.MinimumSolidity ? FormattableString.Invariant($"not compact, hull solidity {solidity:0.00}")
                : aspect > options.MaximumAspect && !elongated ? FormattableString.Invariant($"elongated, aspect {aspect:0.00}")
                : null;
            string? why = shape
                ?? (zone is not null ? $"inside {zone.Name}"
                : !cells.Any(c => Math.Abs(page.X - c.X) <= c.HalfWidth && Math.Abs(page.Y - c.Y) <= c.HalfHeight) ? "outside every bull's cell"
                : null);
            if (why is not null)
            {
                rejected.Add(new RejectedBlob(hx, hy, diameterIn, why, shape is null ? zone?.Name : null));
                continue;
            }

            double cx = moments.X, cy = moments.Y;
            double closure = Closure(binary, width, height, cx, cy, diameter);
            if (closure < options.MinimumClosure)
            {
                rejected.Add(new RejectedBlob(hx, hy, diameterIn, FormattableString.Invariant($"open, rim closure {closure:0.00}")));
                continue;
            }

            if (elongated)
            {
                elongatedBlobs.Add((blob, moments, areaIn, hx, hy, diameterIn, solidity, closure, calibreHoles));
                continue;
            }

            // A blob the size says holds two or more holes, and whose shape gives no split, is reported as it is rather than cut to agree.
            holes.Add(new RenderDifferenceHole(cx, cy, hx, hy, diameterIn, solidity, moments.Ink > 0.3, closure, moments.Elongation, CalibreHoles: calibreHoles, InkFraction: moments.Ink,
                AreaInches: areaIn, Aspect: aspect));
        }

        // S8, the split, decided once the size of a single hole is known (NOTES-FROM-PLANNING.md entries 78, 81 and 82). An elongated blob
        // with less area than SplitMinimumHoles such holes is not two holes, so the size can stop a split but never make one. Such a blob is
        // kept as one hole unless it is also at least ResidueElongation long, which no real hole in the corpus came near (at most 1.72): then
        // it is a sliver of photographed residue and is refused (entry 78 section 2). The size is graded, SizeReference.
        var reference = SizeReference([.. holes.Select(h => h.DiameterInches)], options);
        foreach (var (blob, moments, areaIn, hx, hy, diameterIn, solidity, closure, calibreHoles) in elongatedBlobs)
        {
            double sizeHoles = areaIn / (Math.PI * Math.Pow(reference.VetoInches / 2, 2));
            bool vetoed = sizeHoles < options.SplitMinimumHoles;
            if (vetoed && moments.Elongation >= options.ResidueElongation)
            {
                rejected.Add(new RejectedBlob(hx, hy, diameterIn,
                    FormattableString.Invariant($"residue: elongation {moments.Elongation:0.00} with the area of {sizeHoles:0.00} holes, too small to be two and longer than a hole")));
                continue;
            }

            if (vetoed)
            {
                holes.Add(new RenderDifferenceHole(moments.X, moments.Y, hx, hy, diameterIn, solidity, moments.Ink > 0.3, closure, moments.Elongation, CalibreHoles: calibreHoles, SplitVetoed: true, InkFraction: moments.Ink,
                    AreaInches: areaIn, Aspect: BoxAspect(blob)));
                continue;
            }

            foreach (var (mx, my) in Split(residual, width, blob, moments))
            {
                holes.Add(new RenderDifferenceHole(mx, my, hx, hy, diameterIn, solidity, moments.Ink > 0.3, closure, moments.Elongation, PossibleMerge: true, CalibreHoles: calibreHoles, InkFraction: moments.Ink,
                    AreaInches: areaIn, Aspect: BoxAspect(blob)));
            }
        }

        // S8: a whole mark with the area of OversizeHoles single holes or more is flagged, never cut (NOTES-FROM-PLANNING.md entry 81
        // section 2), and tentatively where the size came from too few marks to trust (entry 82 section 7).
        if (reference.FlagInches is { } flagSize)
        {
            bool tentative = reference.Source == HoleSizeSource.SheetTentative;
            for (int k = 0; k < holes.Count; k++)
            {
                double holesOf = Math.Pow(holes[k].DiameterInches / flagSize, 2);
                holes[k] = holes[k] with { SizeHoles = holesOf };
                if (!holes[k].PossibleMerge && holesOf >= options.OversizeHoles)
                {
                    holes[k] = holes[k] with { Oversized = true, OversizeTentative = tentative };
                }
            }
        }

        return new RenderDifferenceResult(dpi, inkFraction, threshold, holes, rejected, shifts, expected, reference);
    }

    /// <summary>Image pixels per page inch at an image point, from the registration's local area scale.</summary>
    /// <summary>The long side of a blob's bounding box over its short side: the shape number of NOTES-FROM-PLANNING.md entry 88 section 1.</summary>
    internal static double BoxAspect(ImageBlob blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        return Math.Max(blob.Width, blob.Height) / Math.Max(1.0, Math.Min(blob.Width, blob.Height));
    }

    internal static double LocalPixelsPerInch(IPageMapping registration, PointD image)
    {
        var (xx, xy, yx, yy) = registration.Jacobian(image);
        return 254 / Math.Sqrt(Math.Abs((xx * yy) - (xy * yx)));
    }

    /// <summary>
    /// The size of a single hole, graded by what supports it (NOTES-FROM-PLANNING.md entries 81 and 82).
    /// <list type="bullet">
    /// <item><b>A calibre</b> named is used as it is, for the veto and the flags.</item>
    /// <item><b>Without one, the sheet's quarter-point round mark</b>, clamped to the sizes a bullet hole can have, SmallestHoleInches to
    /// LargestHoleInches, so no reference outside what a bullet makes is ever adopted. From MarksForSheetSize round marks it is trusted for
    /// both. From MarksForTentativeSize it only flags, tentatively, because the quarter-point of a handful of marks is little better than
    /// a guess, and the veto falls back to the bound.</item>
    /// <item><b>Two sizes.</b> Where the round marks fall clearly into two groups, no one size fits: a sheet shot with two calibres, or one
    /// with as many merged pairs as single holes. Nothing is flagged, the veto falls back to the bound, and the description asks for the
    /// calibre, one sentence rather than a flag on half the sheet (entry 82 section 3).</item>
    /// <item><b>The bound alone</b> otherwise: the smallest hole any bullet makes. A blob with less area than SplitMinimumHoles of those is
    /// not two holes of any calibre, so the veto still works on a sheet with no holes to learn from, which is where the residue is worst.
    /// It flags nothing, because every real hole is larger than the smallest.</item>
    /// </list>
    /// </summary>
    internal static HoleSizeReference SizeReference(IReadOnlyList<double> roundDiameters, RenderDifferenceOptions options)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        int n = roundDiameters.Count;
        if (options.CalibreInches is { } calibre)
        {
            return new HoleSizeReference(HoleSizeSource.Calibre, calibre, calibre, n, string.Create(inv, $"a hole is taken as {calibre:0.000} in, from the calibre named"));
        }

        double bound = options.SmallestHoleInches;
        if (n < options.MarksForTentativeSize)
        {
            return new HoleSizeReference(HoleSizeSource.Bound, bound, null, n, string.Create(inv,
                $"with {n} round mark{(n == 1 ? "" : "s")} and no calibre, a hole is only known to be at least {bound:0.00} in, so no mark is judged oversized"));
        }

        var sorted = roundDiameters.Order().ToList();
        double quarter = Math.Clamp(sorted[n / 4], bound, options.LargestHoleInches);
        if (n >= options.MarksForSheetSize && TwoSizes(sorted) is { } groups)
        {
            return new HoleSizeReference(HoleSizeSource.TwoSizes, bound, null, n, string.Create(inv,
                $"the marks fall into two sizes, about {groups.Small:0.00} and {groups.Large:0.00} in across, so no one hole size fits this sheet: name the calibre to have oversized marks flagged"));
        }

        return n >= options.MarksForSheetSize
            ? new HoleSizeReference(HoleSizeSource.Sheet, quarter, quarter, n, string.Create(inv, $"a hole is taken as {quarter:0.000} in, the quarter-point of {n} round marks"))
            : new HoleSizeReference(HoleSizeSource.SheetTentative, bound, quarter, n, string.Create(inv,
                $"a hole is taken as about {quarter:0.000} in from only {n} round marks, so oversized marks are flagged tentatively; name the calibre to be sure"));
    }

    /// <summary>
    /// Two clearly separate sizes among the round marks, or null: the split of the sorted log areas with the largest between-group
    /// variance, each group at least a quarter of the marks, whose group medians differ by at least the oversize ratio in area and whose
    /// gap is at least five pooled standard deviations. A continuous spread of sizes, however wide, is not two sizes: an even spread cut in
    /// half is only about three and a half apart.
    /// </summary>
    internal static (double Small, double Large)? TwoSizes(IReadOnlyList<double> sortedDiameters)
    {
        var logs = sortedDiameters.Select(d => 2 * Math.Log(d)).ToList();
        int n = logs.Count, least = Math.Max(3, (int)Math.Ceiling(n / 4.0));
        int bestCut = -1;
        double bestBetween = 0;
        for (int cut = least; cut <= n - least; cut++)
        {
            double m1 = logs.Take(cut).Average(), m2 = logs.Skip(cut).Average();
            double between = cut * (n - cut) * (m2 - m1) * (m2 - m1);
            if (between > bestBetween)
            {
                (bestBetween, bestCut) = (between, cut);
            }
        }

        if (bestCut < 0)
        {
            return null;
        }

        var small = logs.Take(bestCut).ToList();
        var large = logs.Skip(bestCut).ToList();
        static double Sd(List<double> v)
        {
            double m = v.Average();
            return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / Math.Max(1, v.Count - 1));
        }

        double pooled = Math.Sqrt((Sd(small) * Sd(small) + (Sd(large) * Sd(large))) / 2);
        double gap = large.Average() - small.Average();
        double medianSmall = sortedDiameters[bestCut / 2], medianLarge = sortedDiameters[bestCut + ((n - bestCut) / 2)];
        return gap >= Math.Log(1.35) && gap >= 5 * pooled ? (medianSmall, medianLarge) : null;
    }

    /// <summary>
    /// The expected image aligned to the observed one: each bull's cell shifted by the phase correlation of its expected and
    /// observed patches, both normalised by local paper, where the peak is clear and the shift within reach; everything else by
    /// the median of those shifts. The shifts are held on a grid of 16 pixels.
    /// </summary>
    private static (GrayImage Aligned, IReadOnlyList<PointD> Shifts) Align(GrayImage observed, GrayImage expected, double[] paper, int block, double inkFraction,
        IReadOnlyList<(double X, double Y, double HalfWidth, double HalfHeight)> cells, IPageMapping registration, double maximumShift, IImagingBackend backend)
    {
        int width = observed.Width, height = observed.Height;
        var found = new List<(int Left, int Top, int Right, int Bottom, PointD Shift)>();
        var shifts = new List<PointD>();
        foreach (var cell in cells)
        {
            var corners = new[] { new PointD(cell.X - cell.HalfWidth, cell.Y - cell.HalfHeight), new PointD(cell.X + cell.HalfWidth, cell.Y - cell.HalfHeight), new PointD(cell.X + cell.HalfWidth, cell.Y + cell.HalfHeight), new PointD(cell.X - cell.HalfWidth, cell.Y + cell.HalfHeight) }
                .Select(registration.ToImage).ToList();
            int left = Math.Max(0, (int)Math.Floor(corners.Min(c => c.X))), top = Math.Max(0, (int)Math.Floor(corners.Min(c => c.Y)));
            int right = Math.Min(width - 1, (int)Math.Ceiling(corners.Max(c => c.X))), bottom = Math.Min(height - 1, (int)Math.Ceiling(corners.Max(c => c.Y)));
            int w = right - left + 1, h = bottom - top + 1;
            if (w < 32 || h < 32)
            {
                shifts.Add(new PointD(double.NaN, double.NaN));
                continue;
            }

            var e = new byte[w * h];
            var n = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = ((top + y) * width) + left + x;
                    double p = Math.Max(1, PaperAt(paper, block, width, height, left + x, top + y));
                    e[(y * w) + x] = (byte)Math.Clamp(Math.Round(255 * (1 - ((1 - (expected.Pixels[i] / 255.0)) * (1 - inkFraction)))), 0, 255);
                    n[(y * w) + x] = (byte)Math.Clamp(Math.Round(255 * observed.Pixels[i] / p), 0, 255);
                }
            }

            var (shift, response) = backend.PhaseCorrelate(new GrayImage(w, h, e), new GrayImage(w, h, n));
            bool usable = response >= 0.05 && Math.Sqrt((shift.X * shift.X) + (shift.Y * shift.Y)) <= maximumShift;
            shifts.Add(usable ? shift : new PointD(double.NaN, double.NaN));
            if (usable)
            {
                found.Add((left, top, right, bottom, shift));
            }
        }

        var median = found.Count == 0 ? new PointD(0, 0) : new PointD(Median(found.Select(f => f.Shift.X)), Median(found.Select(f => f.Shift.Y)));
        const int grid = 16;
        int gw = (width + grid - 1) / grid, gh = (height + grid - 1) / grid;
        var sx = new double[gw * gh];
        var sy = new double[gw * gh];
        for (int j = 0; j < gh; j++)
        {
            for (int i = 0; i < gw; i++)
            {
                int cx = (i * grid) + (grid / 2), cy = (j * grid) + (grid / 2);
                var covering = found.FirstOrDefault(f => cx >= f.Left && cx <= f.Right && cy >= f.Top && cy <= f.Bottom);
                var s = covering.Right > 0 ? covering.Shift : median;
                sx[(j * gw) + i] = s.X;
                sy[(j * gw) + i] = s.Y;
            }
        }

        var pixels = new byte[width * height];
        Parallel.For(0, height, y =>
        {
            int j = y / grid;
            for (int x = 0; x < width; x++)
            {
                int g = (j * gw) + (x / grid);
                pixels[(y * width) + x] = Sample(expected, x - sx[g], y - sy[g]);
            }
        });

        return (new GrayImage(width, height, pixels), shifts);
    }

    private static byte Sample(GrayImage image, double x, double y)
    {
        if (!(x >= 0 && y >= 0 && x <= image.Width - 1 && y <= image.Height - 1))
        {
            return 255;
        }

        int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, image.Width - 1), y1 = Math.Min(y0 + 1, image.Height - 1);
        double fx = x - x0, fy = y - y0;
        var p = image.Pixels;
        int w = image.Width;
        return (byte)Math.Clamp(Math.Round(((1 - fy) * (((1 - fx) * p[(y0 * w) + x0]) + (fx * p[(y0 * w) + x1]))) + (fy * (((1 - fx) * p[(y1 * w) + x0]) + (fx * p[(y1 * w) + x1])))), 0, 255);
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        int n = sorted.Length;
        return n % 2 == 1 ? sorted[n / 2] : (sorted[(n / 2) - 1] + sorted[n / 2]) / 2;
    }

    /// <summary>
    /// The image pixels inside the page, NOTES-FROM-PLANNING.md entry 23 section 4: the page's edge in dmm, 64 points a side, mapped into
    /// the image through the registration, and filled row by row by the even-odd rule at pixel centres. The points along each side
    /// follow a photograph's curved mapping as well as a scan's straight one.
    /// </summary>
    internal static bool[] SheetMask(int pageWidth, int pageHeight, IPageMapping registration, int width, int height)
    {
        const int PerSide = 64;
        var edge = new List<PointD>(4 * PerSide);
        for (int side = 0; side < 4; side++)
        {
            for (int k = 0; k < PerSide; k++)
            {
                double t = (double)k / PerSide;
                var page = side switch
                {
                    0 => new PointD(t * pageWidth, 0),
                    1 => new PointD(pageWidth, t * pageHeight),
                    2 => new PointD((1 - t) * pageWidth, pageHeight),
                    _ => new PointD(0, (1 - t) * pageHeight),
                };
                edge.Add(registration.ToImage(page));
            }
        }

        var inside = new bool[width * height];
        Parallel.For(0, height, y =>
        {
            var crossings = new List<double>();
            for (int k = 0; k < edge.Count; k++)
            {
                PointD a = edge[k], b = edge[(k + 1) % edge.Count];
                if ((a.Y <= y) != (b.Y <= y))
                {
                    crossings.Add(a.X + ((y - a.Y) * (b.X - a.X) / (b.Y - a.Y)));
                }
            }

            crossings.Sort();
            for (int c = 0; c + 1 < crossings.Count; c += 2)
            {
                int x0 = Math.Max(0, (int)Math.Ceiling(crossings[c])), x1 = Math.Min(width - 1, (int)Math.Floor(crossings[c + 1]));
                for (int x = x0; x <= x1; x++)
                {
                    inside[(y * width) + x] = true;
                }
            }
        });

        return inside;
    }

    /// <summary>
    /// The observed image with every pixel beyond the sheet set to the sheet's paper level: the 90th percentile of the pixels inside the
    /// sheet that the render calls paper. The caller's image is not changed.
    /// </summary>
    private static GrayImage WithinSheet(GrayImage observed, GrayImage expected, bool[] inside)
    {
        if (Array.TrueForAll(inside, i => i))
        {
            return observed;
        }

        var histogram = new long[256];
        long count = 0;
        for (int i = 0; i < inside.Length; i++)
        {
            if (inside[i] && expected.Pixels[i] >= 250)
            {
                histogram[observed.Pixels[i]]++;
                count++;
            }
        }

        byte paper = 255;
        for (int level = 0, seen = 0; level < 256 && count > 0; level++)
        {
            seen += (int)histogram[level];
            if (seen >= 0.9 * count)
            {
                paper = (byte)level;
                break;
            }
        }

        var pixels = (byte[])observed.Pixels.Clone();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!inside[i])
            {
                pixels[i] = paper;
            }
        }

        return new GrayImage(observed.Width, observed.Height, pixels);
    }

    /// <summary>The 95th percentile of the observed pixels the render calls paper, per block, with empty blocks filled from their neighbours and every block averaged with its eight.</summary>
    private static double[] PaperField(GrayImage observed, GrayImage expected, int block)
    {
        int bw = (observed.Width + block - 1) / block, bh = (observed.Height + block - 1) / block;
        var level = new double[bw * bh];
        Parallel.For(0, bh, j =>
        {
            var histogram = new int[256];
            for (int i = 0; i < bw; i++)
            {
                Array.Clear(histogram);
                int count = 0;
                for (int y = j * block; y < Math.Min(observed.Height, (j + 1) * block); y++)
                {
                    for (int x = i * block; x < Math.Min(observed.Width, (i + 1) * block); x++)
                    {
                        int k = (y * observed.Width) + x;
                        if (expected.Pixels[k] >= 230)
                        {
                            histogram[observed.Pixels[k]]++;
                            count++;
                        }
                    }
                }

                level[(j * bw) + i] = count < 16 ? double.NaN : Quantile(histogram, count, 0.95);
            }
        });

        for (int pass = 0; pass < Math.Max(bw, bh) && level.Any(double.IsNaN); pass++)
        {
            var next = (double[])level.Clone();
            for (int j = 0; j < bh; j++)
            {
                for (int i = 0; i < bw; i++)
                {
                    if (!double.IsNaN(level[(j * bw) + i]))
                    {
                        continue;
                    }

                    var around = Neighbours(level, bw, bh, i, j).Where(v => !double.IsNaN(v)).ToList();
                    next[(j * bw) + i] = around.Count == 0 ? double.NaN : around.Average();
                }
            }

            level = next;
        }

        var smoothed = new double[level.Length];
        for (int j = 0; j < bh; j++)
        {
            for (int i = 0; i < bw; i++)
            {
                smoothed[(j * bw) + i] = Neighbours(level, bw, bh, i, j).Append(level[(j * bw) + i]).Where(v => !double.IsNaN(v)).DefaultIfEmpty(245).Average();
            }
        }

        return smoothed;
    }

    private static IEnumerable<double> Neighbours(double[] level, int bw, int bh, int i, int j)
    {
        for (int dj = -1; dj <= 1; dj++)
        {
            for (int di = -1; di <= 1; di++)
            {
                if ((di != 0 || dj != 0) && i + di >= 0 && i + di < bw && j + dj >= 0 && j + dj < bh)
                {
                    yield return level[((j + dj) * bw) + i + di];
                }
            }
        }
    }

    private static double PaperAt(double[] level, int block, int width, int height, int x, int y)
    {
        int bw = (width + block - 1) / block, bh = (height + block - 1) / block;
        double fx = ((x + 0.5) / block) - 0.5, fy = ((y + 0.5) / block) - 0.5;
        int i0 = Math.Clamp((int)Math.Floor(fx), 0, bw - 1), j0 = Math.Clamp((int)Math.Floor(fy), 0, bh - 1);
        int i1 = Math.Min(i0 + 1, bw - 1), j1 = Math.Min(j0 + 1, bh - 1);
        double tx = Math.Clamp(fx - i0, 0, 1), ty = Math.Clamp(fy - j0, 0, 1);
        return ((1 - ty) * (((1 - tx) * level[(j0 * bw) + i0]) + (tx * level[(j0 * bw) + i1]))) + (ty * (((1 - tx) * level[(j1 * bw) + i0]) + (tx * level[(j1 * bw) + i1])));
    }

    /// <summary>The median ratio of observed to local paper where the render calls solid ink.</summary>
    private static double InkFraction(GrayImage observed, GrayImage expected, double[] paper, int block)
    {
        var histogram = new int[1001];
        int count = 0;
        for (int y = 0; y < observed.Height; y += 2)
        {
            for (int x = 0; x < observed.Width; x += 2)
            {
                int k = (y * observed.Width) + x;
                if (expected.Pixels[k] <= 20)
                {
                    double ratio = observed.Pixels[k] / Math.Max(1, PaperAt(paper, block, observed.Width, observed.Height, x, y));
                    histogram[Math.Clamp((int)Math.Round(ratio * 1000), 0, 1000)]++;
                    count++;
                }
            }
        }

        if (count == 0)
        {
            return 0.25;
        }

        int seen = 0;
        for (int b = 0; b <= 1000; b++)
        {
            seen += histogram[b];
            if (seen * 2 >= count)
            {
                return b / 1000.0;
            }
        }

        return 0.25;
    }

    private static double Quantile(int[] histogram, int count, double q)
    {
        long target = Math.Max(1, (long)Math.Ceiling(q * count)), seen = 0;
        for (int i = 0; i < histogram.Length; i++)
        {
            seen += histogram[i];
            if (seen >= target)
            {
                return i;
            }
        }

        return histogram.Length - 1;
    }

    private readonly record struct BlobMoments(double X, double Y, double Ink, double Elongation, double AxisX, double AxisY, double Major);

    /// <summary>
    /// The residual-weighted centroid inside the hull, the mean expected ink coverage there, and the weighted second moments:
    /// the ratio of the principal standard deviations, the major axis, and the standard deviation along it, pixels.
    /// </summary>
    private static BlobMoments Moments(byte[] residual, GrayImage expected, int width, ImageBlob blob)
    {
        double sw = 0, sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0, ink = 0;
        int n = 0;
        for (int y = blob.Top; y < blob.Top + blob.Height; y++)
        {
            for (int x = blob.Left; x < blob.Left + blob.Width; x++)
            {
                if (!Inside(blob.Hull, x, y))
                {
                    continue;
                }

                int i = (y * width) + x;
                double weight = residual[i];
                sw += weight;
                sx += weight * x;
                sy += weight * y;
                sxx += weight * x * x;
                syy += weight * y * y;
                sxy += weight * x * y;
                ink += 1 - (expected.Pixels[i] / 255.0);
                n++;
            }
        }

        if (sw == 0)
        {
            return new BlobMoments(blob.Left + (blob.Width / 2.0), blob.Top + (blob.Height / 2.0), 0, 1, 1, 0, 0);
        }

        double mx = sx / sw, my = sy / sw;
        double a = (sxx / sw) - (mx * mx), c = (syy / sw) - (my * my), b = (sxy / sw) - (mx * my);
        double mean = (a + c) / 2, root = Math.Sqrt((((a - c) / 2) * ((a - c) / 2)) + (b * b));
        double major = Math.Max(0, mean + root), minor = Math.Max(1e-9, mean - root), angle = 0.5 * Math.Atan2(2 * b, a - c);
        return new BlobMoments(mx, my, ink / Math.Max(1, n), Math.Sqrt(major / minor), Math.Cos(angle), Math.Sin(angle), Math.Sqrt(major));
    }

    /// <summary>Two centres by residual-weighted two-means over the hull, started one standard deviation either side of the centroid along the major axis.</summary>
    private static (double X, double Y)[] Split(byte[] residual, int width, ImageBlob blob, BlobMoments m)
    {
        var centres = new (double X, double Y)[] { (m.X - (m.Major * m.AxisX), m.Y - (m.Major * m.AxisY)), (m.X + (m.Major * m.AxisX), m.Y + (m.Major * m.AxisY)) };
        for (int iteration = 0; iteration < 20; iteration++)
        {
            var sw = new double[2];
            var sx = new double[2];
            var sy = new double[2];
            for (int y = blob.Top; y < blob.Top + blob.Height; y++)
            {
                for (int x = blob.Left; x < blob.Left + blob.Width; x++)
                {
                    double weight = residual[(y * width) + x];
                    if (weight == 0 || !Inside(blob.Hull, x, y))
                    {
                        continue;
                    }

                    int k = Math.Pow(x - centres[0].X, 2) + Math.Pow(y - centres[0].Y, 2) <= Math.Pow(x - centres[1].X, 2) + Math.Pow(y - centres[1].Y, 2) ? 0 : 1;
                    sw[k] += weight;
                    sx[k] += weight * x;
                    sy[k] += weight * y;
                }
            }

            for (int k = 0; k < 2; k++)
            {
                if (sw[k] > 0)
                {
                    centres[k] = (sx[k] / sw[k], sy[k] / sw[k]);
                }
            }
        }

        return centres;
    }

    /// <summary>The fraction of 360 rays from the centre, one pixel steps, that meet a foreground pixel of <paramref name="binary"/> within <paramref name="reach"/> pixels.</summary>
    private static double Closure(byte[] binary, int width, int height, double cx, double cy, double reach)
    {
        int closed = 0;
        for (int k = 0; k < 360; k++)
        {
            double dx = Math.Cos(k * Math.PI / 180), dy = Math.Sin(k * Math.PI / 180);
            for (double r = 0; r <= reach; r++)
            {
                int x = (int)Math.Round(cx + (r * dx)), y = (int)Math.Round(cy + (r * dy));
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    break;
                }

                if (binary[(y * width) + x] != 0)
                {
                    closed++;
                    break;
                }
            }
        }

        return closed / 360.0;
    }

    private static bool Inside(IReadOnlyList<PointD> hull, double x, double y)
    {
        int sign = 0;
        for (int i = 0; i < hull.Count; i++)
        {
            var a = hull[i];
            var b = hull[(i + 1) % hull.Count];
            double cross = ((b.X - a.X) * (y - a.Y)) - ((b.Y - a.Y) * (x - a.X));
            if (cross == 0)
            {
                continue;
            }

            int s = Math.Sign(cross);
            if (sign == 0)
            {
                sign = s;
            }
            else if (s != sign)
            {
                return false;
            }
        }

        return true;
    }

    private static (double Area, double X, double Y) Polygon(IReadOnlyList<PointD> hull)
    {
        double a = 0, sx = 0, sy = 0;
        for (int i = 0; i < hull.Count; i++)
        {
            var p = hull[i];
            var q = hull[(i + 1) % hull.Count];
            double cross = (p.X * q.Y) - (q.X * p.Y);
            a += cross;
            sx += (p.X + q.X) * cross;
            sy += (p.Y + q.Y) * cross;
        }

        return a == 0 ? (0, double.NaN, double.NaN) : (Math.Abs(a) / 2, sx / (3 * a), sy / (3 * a));
    }

    /// <summary>
    /// The declared exclusion zones of stage S5, page dmm: every marker's footprint with its quiet zone, every code's modules
    /// with its quiet zone, the data block, and the identifier text.
    /// </summary>
    private static List<Zone> Zones(TargetDefinition definition, Scene scene, int tile)
    {
        var zones = new List<Zone>();
        if (definition.Fiducials is { } fiducials)
        {
            double half = (fiducials.MarkerSize / 2.0) + fiducials.QuietZone;
            zones.AddRange(PageRegistration.ExpectedMarkers(definition, tile).Select(m => new Zone($"marker {m.Id}", m.X - half, m.Y - half, m.X + half, m.Y + half)));
        }

        var codeItems = scene.Items.OfType<RectFill>().Where(r => r.Layer == SceneLayer.Codes).ToList();
        if (definition.Codes is { } codes && codeItems.Count > 0)
        {
            double quiet = (codes.QuietZone ?? 4) * codes.ModuleSize;
            foreach (var group in codeItems.GroupBy(r => codes.Positions.Select((p, k) => (k, Distance: Math.Pow((r.X / 2.0) - p.X, 2) + Math.Pow((r.Y / 2.0) - p.Y, 2))).MinBy(t => t.Distance).k))
            {
                zones.Add(new Zone($"code {group.Key}", group.Min(r => r.X / 2.0) - quiet, group.Min(r => r.Y / 2.0) - quiet, group.Max(r => (r.X + r.Width) / 2.0) + quiet, group.Max(r => (r.Y + r.Height) / 2.0) + quiet));
            }
        }

        if (definition.DataBlock is { } data)
        {
            zones.Add(new Zone("the data block", data.X, data.Y, data.X + data.Width, data.Y + data.Height));
        }

        foreach (var text in scene.Items.OfType<TextRun>().Where(t => t.Layer == SceneLayer.Identifier))
        {
            double size = text.FontSize / 2.0, length = 0.6 * size * text.Text.Length, x = text.X / 2.0, baseline = text.Baseline / 2.0;
            double left = text.Anchor switch { TextAnchor.Centre => x - (length / 2), TextAnchor.Right => x - length, _ => x };
            zones.Add(new Zone("the identifier", left - 10, baseline - size - 10, left + length + 10, baseline + (0.3 * size) + 10));
        }

        return zones;
    }
}
