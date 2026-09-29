using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Capture;

/// <summary>What a camera frame showed of a sheet before GroupLab knew which sheet it is.</summary>
/// <param name="Definition">The sheet, from its codes or from its markers' layout; null while neither names one.</param>
/// <param name="FromCodes">Whether the codes named it, which is certain; the markers' layout is only good enough to guide the camera.</param>
/// <param name="MarkersFound">Markers decoded in the frame.</param>
/// <param name="MedianSidePixels">Their median side, in frame pixels; 0 with none.</param>
/// <param name="CodesRead">Square codes read in the frame.</param>
public sealed record LiveSearch(TargetDefinition? Definition, bool FromCodes, int MarkersFound, double MedianSidePixels, int CodesRead);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 260: which sheet the camera is looking at, from one frame of the analysis stream. The codes are tried first,
/// since a code that passes its check names exactly one definition (<see cref="SheetIdentification"/>). But a code's modules are about a
/// pixel each at a camera's analysis resolution unless the sheet fills the frame, so on 2026-09-28 the Fold 7 never read one, never knew the
/// sheet, and the capture screen said "Move back" to a sheet lying in plain view. So the markers are tried as well: each library sheet whose
/// markers carry the ids found is fitted by a homography from its marker centres to the found ones, and the sheet fitting the most markers
/// most closely is taken. Sheets that share ids share their layout, so the one chosen is the right shape to guide the camera by; the picture
/// itself is identified from its codes at full resolution afterwards.
/// </summary>
public static class LiveSheet
{
    /// <summary>The fewest markers a layout is fitted from.</summary>
    public const int LeastMarkers = 6;

    /// <summary>A layout fits when its markers land, on average, within this share of a marker's side of where they were found.</summary>
    public const double FitWithinSides = 0.5;

    /// <summary>
    /// The marker side, in pixels, below which a sheet's codes are too small to read. A built-in sheet's marker is 40 dmm and its code's
    /// module 4 dmm, so a module is a tenth of a marker's side, and at 20 pixels a side a module gets 2 pixels, about the least a QR
    /// reader decodes from.
    /// </summary>
    public const double ReadableSidePixels = 20;

    /// <summary>The longest page among the built-in sheets, in dmm, Letter's 11 in, for the first guess at a marker's size.</summary>
    private const double LongestPageDmm = 2794;

    public static LiveSearch Find(GrayImage frame, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(backend);

        // The first guess sizes the detector for a Letter sheet across half the frame, as the measurer does for a photograph.
        double guess = 0.5 * Math.Max(frame.Width, frame.Height) / LongestPageDmm * 40;
        var found = backend.DetectMarkers(frame, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess)).Markers;
        double median = found.Count == 0 ? 0 : found.Select(Side).Order().ElementAt(found.Count / 2);

        var codes = backend.ReadCodes(frame, 1.0);
        var named = codes.Select(p => GltdBinary.Decode([p]).DefinitionId).OfType<string>().Distinct(StringComparer.Ordinal).ToList();
        if (named.Count == 1 && candidates.FirstOrDefault(c => GltdBinary.Encode(c).Encoding?.DefinitionId == named[0]) is { } fromCodes)
        {
            return new LiveSearch(fromCodes, true, found.Count, median, codes.Count);
        }

        return new LiveSearch(found.Count >= LeastMarkers ? ByLayout(found, median, candidates) : null, false, found.Count, median, codes.Count);
    }

    /// <summary>The candidate whose marker layout fits the found markers best: most markers fitted, then the closest fit.</summary>
    public static TargetDefinition? ByLayout(IReadOnlyList<DetectedMarker> found, double sidePixels, IReadOnlyList<TargetDefinition> candidates) =>
        Fits(found, sidePixels, candidates).OrderByDescending(f => f.Count).ThenBy(f => f.Residual).Select(f => f.Definition).FirstOrDefault();

    /// <summary>How much further than the best a fit may be and still count as the same layout: sheets sharing a layout fit identically.</summary>
    public const double SameLayoutResidual = 0.02;

    /// <summary>
    /// Entry 281: the sheets a picture's markers name when its codes cannot be read. The camera test of 2026-09-29 had three pictures of six
    /// refused because a code's 0.4 mm modules got about 3 pixels each, while the markers named the sheet on every frame. Every candidate that
    /// fits as many markers as the best and as closely, within <see cref="SameLayoutResidual"/>: one where the layout is the sheet's own,
    /// several where variants share a layout, and then the person chooses among those.
    /// </summary>
    public static IReadOnlyList<TargetDefinition> SheetsByMarkers(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(backend);
        double guess = 0.5 * Math.Max(image.Width, image.Height) / LongestPageDmm * 40;
        var found = backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess)).Markers;
        if (found.Count < LeastMarkers)
        {
            return [];
        }

        double median = found.Select(Side).Order().ElementAt(found.Count / 2);
        var fits = Fits(found, median, candidates);
        if (fits.Count == 0)
        {
            return [];
        }

        int most = fits.Max(f => f.Count);
        double closest = fits.Where(f => f.Count == most).Min(f => f.Residual);
        return [.. fits.Where(f => f.Count == most && f.Residual <= closest + SameLayoutResidual).Select(f => f.Definition)];
    }

    /// <summary>The room cut around a code, in dmm beyond its own corner on every side: its quiet zone and the registration's error.</summary>
    public const int CodeMarginDmm = 250;

    /// <summary>
    /// Entry 282 section 5: each code of a picture, cut out where the markers say it is. In the camera test a code's module got about 3.1
    /// pixels, too few to read at the picture's size or doubled, and both codes read at three times; the whole picture cannot be tripled,
    /// which would pass <see cref="Registration.SheetIdentification.MaximumWorkingSide"/>, so only the codes are. The places come from every
    /// sheet sharing the layout the markers fit, since variants of one layout put their codes in the same places.
    /// </summary>
    public static IReadOnlyList<GrayImage> CodeCrops(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(backend);
        var sameLayout = SheetsByMarkers(image, candidates, backend);
        if (sameLayout.Count == 0)
        {
            return [];
        }

        double guess = 0.5 * Math.Max(image.Width, image.Height) / LongestPageDmm * 40;
        var found = backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess)).Markers;
        var crops = new List<GrayImage>();
        var done = new HashSet<(int, int, int)>();
        foreach (var candidate in sameLayout)
        {
            if (candidate.Codes is not { } codes)
            {
                continue;
            }

            var byId = (candidate.Fiducials?.Markers ?? []).GroupBy(m => m.Id).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
            var page = new List<PointD>();
            var seen = new List<PointD>();
            foreach (var marker in found.Where(m => byId.ContainsKey(m.Id)))
            {
                page.Add(new PointD(byId[marker.Id].X, byId[marker.Id].Y));
                seen.Add(Centre(marker));
            }

            if (page.Count < LeastMarkers || HomographyEstimate.Fit(page, seen) is not { } pageToImage)
            {
                continue;
            }

            int side = (codes.Version is { } v ? (4 * v) + 17 : 57) * codes.ModuleSize;
            foreach (var at in codes.Positions)
            {
                if (!done.Add((at.X, at.Y, side)))
                {
                    continue;
                }

                var corners = new[] { new PointD(at.X - CodeMarginDmm, at.Y - CodeMarginDmm), new PointD(at.X + side + CodeMarginDmm, at.Y - CodeMarginDmm),
                    new PointD(at.X + side + CodeMarginDmm, at.Y + side + CodeMarginDmm), new PointD(at.X - CodeMarginDmm, at.Y + side + CodeMarginDmm) }.Select(pageToImage.Apply).ToList();
                int x0 = (int)Math.Max(0, corners.Min(c => c.X)), y0 = (int)Math.Max(0, corners.Min(c => c.Y));
                int x1 = (int)Math.Min(image.Width, Math.Ceiling(corners.Max(c => c.X))), y1 = (int)Math.Min(image.Height, Math.Ceiling(corners.Max(c => c.Y)));
                if (x1 - x0 < 16 || y1 - y0 < 16)
                {
                    continue;
                }

                var pixels = new byte[(x1 - x0) * (y1 - y0)];
                for (int y = y0; y < y1; y++)
                {
                    Array.Copy(image.Pixels, (y * image.Width) + x0, pixels, (y - y0) * (x1 - x0), x1 - x0);
                }

                crops.Add(new GrayImage(x1 - x0, y1 - y0, pixels));
            }
        }

        return crops;
    }

    /// <summary>The resolution the candidates are compared at, dots an inch: enough to tell a bull's artwork and a load block apart.</summary>
    public const double CompareDpi = 40;

    /// <summary>How much better the most alike must correlate than the next for GroupLab to choose it rather than ask.</summary>
    public const double ClearlyMoreAlike = 0.03;

    /// <summary>
    /// Entry 281: among sheets that share a marker layout, the one a picture shows. On the camera test's pictures seven library sheets
    /// fitted the markers equally (the 5x5 sheets with and without the load block, their C and E bull versions, and the 5x6), so the markers
    /// cannot choose. The picture is laid onto the page through the markers they share and compared with each candidate's own drawing at
    /// <see cref="CompareDpi"/>, by correlation; the most alike is returned where it beats the next by <see cref="ClearlyMoreAlike"/>, and
    /// null otherwise, when the person chooses. Holes and handwriting are in the picture and in no drawing, so they cost every candidate alike.
    /// </summary>
    public static TargetDefinition? MostAlike(GrayImage image, IReadOnlyList<TargetDefinition> sameLayout, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(sameLayout);
        ArgumentNullException.ThrowIfNull(backend);
        if (sameLayout.Count <= 1)
        {
            return sameLayout.FirstOrDefault();
        }

        double guess = 0.5 * Math.Max(image.Width, image.Height) / LongestPageDmm * 40;
        var found = backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess)).Markers;
        var scored = new List<(TargetDefinition Definition, double Correlation)>();
        foreach (var candidate in sameLayout)
        {
            var byId = (candidate.Fiducials?.Markers ?? []).GroupBy(m => m.Id).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
            var page = new List<PointD>();
            var seen = new List<PointD>();
            foreach (var marker in found.Where(m => byId.ContainsKey(m.Id)))
            {
                page.Add(new PointD(byId[marker.Id].X, byId[marker.Id].Y));
                seen.Add(Centre(marker));
            }

            if (page.Count < LeastMarkers || HomographyEstimate.Fit(page, seen) is not { } pageToImage)
            {
                continue;
            }

            double dmmPerPixel = 254 / CompareDpi;
            int w = (int)(candidate.Page.Width / dmmPerPixel), h = (int)(candidate.Page.Height / dmmPerPixel);
            var toRectified = Homography.Compose(pageToImage.Inverse(), new Homography([1 / dmmPerPixel, 0, 0, 0, 1 / dmmPerPixel, 0, 0, 0, 1]));
            var rectified = PortableImaging.WarpPerspective(image, toRectified, w, h);
            var drawing = Rendering.SceneRasterizer.Rasterize(Rendering.SceneBuilder.Build(candidate).Pages[0], CompareDpi);
            scored.Add((candidate, Correlation(rectified, drawing)));
        }

        var ranked = scored.OrderByDescending(s => s.Correlation).ToList();
        return ranked.Count > 0 && (ranked.Count == 1 || ranked[0].Correlation - ranked[1].Correlation >= ClearlyMoreAlike) ? ranked[0].Definition : null;
    }

    private static double Correlation(GrayImage a, GrayImage b)
    {
        int w = Math.Min(a.Width, b.Width), h = Math.Min(a.Height, b.Height);
        double sa = 0, sb = 0, saa = 0, sbb = 0, sab = 0;
        int n = w * h;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double p = a.Pixels[(y * a.Width) + x], q = b.Pixels[(y * b.Width) + x];
                (sa, sb, saa, sbb, sab) = (sa + p, sb + q, saa + (p * p), sbb + (q * q), sab + (p * q));
            }
        }

        double cov = sab - (sa * sb / n), va = saa - (sa * sa / n), vb = sbb - (sb * sb / n);
        return va > 0 && vb > 0 ? cov / Math.Sqrt(va * vb) : 0;
    }

    private static List<(TargetDefinition Definition, int Count, double Residual)> Fits(IReadOnlyList<DetectedMarker> found, double sidePixels, IReadOnlyList<TargetDefinition> candidates)
    {
        ArgumentNullException.ThrowIfNull(found);
        ArgumentNullException.ThrowIfNull(candidates);
        var fits = new List<(TargetDefinition, int, double)>();
        foreach (var candidate in candidates)
        {
            if (candidate.Fiducials?.Markers is not { Count: > 0 } markers)
            {
                continue;
            }

            var byId = markers.GroupBy(m => m.Id).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
            var page = new List<PointD>();
            var image = new List<PointD>();
            foreach (var marker in found)
            {
                if (byId.TryGetValue(marker.Id, out var at))
                {
                    page.Add(new PointD(at.X, at.Y));
                    image.Add(Centre(marker));
                }
            }

            if (page.Count < LeastMarkers || HomographyEstimate.Fit(page, image) is not { } fit)
            {
                continue;
            }

            double residual = Math.Sqrt(page.Select((p, i) => Squared(fit.Apply(p), image[i])).Average()) / Math.Max(1, sidePixels);
            if (residual <= FitWithinSides)
            {
                fits.Add((candidate, page.Count, residual));
            }
        }

        return fits;
    }

    private static double Squared(PointD a, PointD b) => ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y));

    private static PointD Centre(DetectedMarker marker) =>
        new(marker.Corners.Average(c => c.X), marker.Corners.Average(c => c.Y));

    private static double Side(DetectedMarker marker) =>
        Enumerable.Range(0, marker.Corners.Count).Average(i => Math.Sqrt(Squared(marker.Corners[i], marker.Corners[(i + 1) % marker.Corners.Count])));
}
