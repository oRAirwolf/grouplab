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
/// <summary>A code of a picture turned square on (<see cref="LiveSheet.CodeViews"/>), with where its centre is in the picture and its width there, in pixels.</summary>
public sealed record CodeView(GrayImage Image, PointD Centre, double Side)
{
    /// <summary>Whether <paramref name="other"/> shows the same code: its centre lies within a code's width of this one's, where a sheet's codes are a page apart.</summary>
    public bool SameCodeAs(CodeView other)
    {
        ArgumentNullException.ThrowIfNull(other);
        double dx = other.Centre.X - Centre.X, dy = other.Centre.Y - Centre.Y;
        return (dx * dx) + (dy * dy) < Side * Side;
    }
}

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

    /// <summary>
    /// The markers of a photograph whose scale is not known: sized first for a Letter sheet across half the picture, as the measurer does,
    /// and where that decodes none, for one across a quarter of it (<see cref="Measurement.SheetMeasurer.FarGuess"/>). Entry 322 section 1:
    /// a sheet 2 ft from the phone gave markers too small for the first guess's size gates, and none was read although every one could be.
    /// </summary>
    public static IReadOnlyList<DetectedMarker> PhotographMarkers(GrayImage image, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(backend);
        double guess = 0.5 * Math.Max(image.Width, image.Height) / LongestPageDmm * 40;
        var found = backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess)).Markers;
        return found.Count > 0
            ? found
            : backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess * Measurement.SheetMeasurer.FarGuess)).Markers;
    }

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
        return SheetsByMarkers(PhotographMarkers(image, backend), candidates);
    }

    /// <summary>Entry 400: the same, from markers already found in the picture, so a caller that needs them too finds them once.</summary>
    private static IReadOnlyList<TargetDefinition> SheetsByMarkers(IReadOnlyList<DetectedMarker> found, IReadOnlyList<TargetDefinition> candidates)
    {
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
        var crops = new List<GrayImage>();
        foreach (var (pageToImage, at, side, _) in CodePlaces(image, candidates, backend))
        {
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

        return crops;
    }

    /// <summary>The pixels a code's module gets in <see cref="CodeViews"/>: twice the three a phone picture gives, and what a QR reader reads best.</summary>
    public const int ViewPixelsPerModule = 6;

    /// <summary>The room kept around a code in <see cref="CodeViews"/>, in modules on every side: its quiet zone of four and the fit's error.</summary>
    public const int ViewMarginModules = 16;

    /// <summary>The markers nearest a code whose corners place it for <see cref="CodeViews"/>.</summary>
    public const int NearestMarkers = 6;

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 291 section 3.1: each code of a picture, cut out where the markers say it is and turned square on, at
    /// <see cref="ViewPixelsPerModule"/> pixels a module. A picture taken at an angle drew its codes as trapezia a few pixels a module, which
    /// the reader missed in the whole picture at four resolutions and found only in <see cref="CodeCrops"/> enlarged: 20 to 22 seconds on
    /// this computer and 14 to 30 on the Fold 7. Square on and small, each code is read once, in tens of milliseconds. Each view says where
    /// in the picture it was taken and how wide the code is there, so views of one code, from sheets that share a layout, count once.
    /// </summary>
    public static IReadOnlyList<CodeView> CodeViews(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend)
    {
        var views = new List<CodeView>();
        foreach (var (pageToImage, at, side, module) in CodePlaces(image, candidates, backend))
        {
            double margin = ViewMarginModules * module;
            double perDmm = (double)ViewPixelsPerModule / module;
            int size = (int)Math.Ceiling((side + (2 * margin)) * perDmm);
            var viewToPage = new Homography([1 / perDmm, 0, at.X - margin, 0, 1 / perDmm, at.Y - margin, 0, 0, 1]);
            var viewToImage = Homography.Compose(viewToPage, pageToImage);
            var centre = viewToImage.Apply(new PointD(size / 2.0, size / 2.0));
            if (centre.X < 0 || centre.Y < 0 || centre.X >= image.Width || centre.Y >= image.Height)
            {
                continue;
            }

            double across = Math.Sqrt(Squared(pageToImage.Apply(at), pageToImage.Apply(new PointD(at.X + side, at.Y))));
            views.Add(new CodeView(backend.WarpPerspective(image, viewToImage.Inverse(), size, size), centre, across));
        }

        return views;
    }

    /// <summary>
    /// Where each code of every sheet sharing the layout the markers fit lies: the page-to-picture transform fitted from the markers, the
    /// code's corner and side on the page, and its module, in dmm. Variants of one layout put their codes in the same places, so each place
    /// is given once.
    /// </summary>
    private static List<(Homography PageToImage, PointD At, int Side, int Module)> CodePlaces(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(backend);
        var places = new List<(Homography, PointD, int, int)>();
        var found = PhotographMarkers(image, backend);
        var sameLayout = SheetsByMarkers(found, candidates);
        if (sameLayout.Count == 0)
        {
            return places;
        }

        var done = new List<(PointD Centre, double Side)>();
        foreach (var candidate in sameLayout)
        {
            if (candidate.Codes is not { } codes)
            {
                continue;
            }

            // Where the sheet prints its markers and codes, as registration and the renderer derive them.
            var (byId, positions) = Printed(candidate, found);
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

            // A code sits in a corner, outside the markers, where a fit to the whole sheet is extrapolated. So each code is placed by the
            // corners of the markers nearest it, and the whole fit is kept only where those give none.
            double half = (candidate.Fiducials?.MarkerSize ?? 40) / 2.0;
            var matched = found.Where(m => byId.ContainsKey(m.Id) && m.Corners.Count == 4).ToList();
            int side = (codes.Version is { } v ? (4 * v) + 17 : 57) * codes.ModuleSize;
            foreach (var at in positions)
            {
                var near = matched.OrderBy(m => Squared(new PointD(byId[m.Id].X, byId[m.Id].Y), new PointD(at.X, at.Y))).Take(NearestMarkers).ToList();
                var nearPage = near.SelectMany(m => new[] { new PointD(byId[m.Id].X - half, byId[m.Id].Y - half), new PointD(byId[m.Id].X + half, byId[m.Id].Y - half),
                    new PointD(byId[m.Id].X + half, byId[m.Id].Y + half), new PointD(byId[m.Id].X - half, byId[m.Id].Y + half) }).ToList();
                var nearSeen = near.SelectMany(m => m.Corners).ToList();
                var fit = (near.Count >= NearestMarkers ? HomographyEstimate.Fit(nearPage, nearSeen) : null) ?? pageToImage;

                // Entry 291 section 3.1: sheets sharing a layout share their markers' ids and spacing, not where their codes sit beside them:
                // the frozen Phase 0 sheet fits the same markers as sheets whose codes are 7 mm higher. So a place is the same place only where
                // it lands on the same part of the picture.
                var centre = fit.Apply(new PointD(at.X, at.Y));
                double across = Math.Sqrt(Squared(fit.Apply(new PointD(at.X - (side / 2.0), at.Y)), fit.Apply(new PointD(at.X + (side / 2.0), at.Y))));
                if (done.Any(d => Squared(d.Centre, centre) < 0.0625 * d.Side * d.Side))
                {
                    continue;
                }

                done.Add((centre, across));
                places.Add((fit, new PointD(at.X - (side / 2.0), at.Y - (side / 2.0)), side, codes.ModuleSize));
            }
        }

        return places;
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

        var found = PhotographMarkers(image, backend);
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

            // Crash report 9: a sheet the renderer refuses (one of a person's own that no longer validates) has no page to compare with,
            // and indexing its first page threw inside a reading on the iPad mini. It is passed over, as a sheet the markers do not fit is.
            var pages = Rendering.SceneBuilder.Build(candidate).Pages;
            if (pages.Count == 0)
            {
                continue;
            }

            double dmmPerPixel = 254 / CompareDpi;
            int w = (int)(candidate.Page.Width / dmmPerPixel), h = (int)(candidate.Page.Height / dmmPerPixel);
            var toRectified = Homography.Compose(pageToImage.Inverse(), new Homography([1 / dmmPerPixel, 0, 0, 0, 1 / dmmPerPixel, 0, 0, 0, 1]));
            var rectified = PortableImaging.WarpPerspective(image, toRectified, w, h);
            var drawing = Rendering.SceneRasterizer.Rasterize(pages[0], CompareDpi);
            scored.Add((candidate, Correlation(rectified, drawing)));
        }

        var ranked = scored.OrderByDescending(s => s.Correlation).ToList();
        return ranked.Count > 0 && (ranked.Count == 1 || ranked[0].Correlation - ranked[1].Correlation >= ClearlyMoreAlike) ? ranked[0].Definition : null;
    }

    /// <summary>How alike, by correlation, a sheet's printed name must be to the picture's to name it: words that match, not a blank band.</summary>
    public const double NameAlike = 0.5;

    /// <summary>How much more alike the best printed name must be than the next.</summary>
    public const double NameClearly = 0.1;

    /// <summary>
    /// Entry 356 section 6 item 4: among the sheets whose markers fit the picture's, the one whose printed identifier and title match the
    /// picture's, drawn as the sheet prints them, words and all, compared in their own boxes only; null where none matches clearly. A sheet's
    /// identifier is printed under its title, so two sheets sharing a layout differ there however alike their bulls are.
    /// </summary>
    public static TargetDefinition? ByPrintedName(GrayImage image, IReadOnlyList<TargetDefinition> candidates, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(backend);
        var found = PhotographMarkers(image, backend);
        var fitting = SheetsByMarkers(found, candidates);
        if (fitting.Count == 0)
        {
            return null;
        }

        const double dpi = 100;
        var scored = new List<(TargetDefinition Definition, double Correlation)>();
        foreach (var candidate in fitting)
        {
            var byId = (candidate.Fiducials?.Markers ?? []).GroupBy(m => m.Id).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
            var page = new List<PointD>();
            var seen = new List<PointD>();
            foreach (var marker in found.Where(m => byId.ContainsKey(m.Id)))
            {
                page.Add(new PointD(byId[marker.Id].X, byId[marker.Id].Y));
                seen.Add(Centre(marker));
            }

            var pages = Rendering.SceneBuilder.Build(candidate, new Rendering.RenderOptions(AllowInvalid: true)).Pages;
            if (page.Count < LeastMarkers || HomographyEstimate.Fit(page, seen) is not { } pageToImage || pages.Count == 0)
            {
                continue;
            }

            var words = pages[0].Items.OfType<Rendering.TextRun>().Where(t => t.Layer is Rendering.SceneLayer.Identifier or Rendering.SceneLayer.Name).ToList();
            if (words.Count == 0)
            {
                continue;
            }

            double dmmPerPixel = 254 / dpi;
            int w = (int)(candidate.Page.Width / dmmPerPixel), h = (int)(candidate.Page.Height / dmmPerPixel);
            var toRectified = Homography.Compose(pageToImage.Inverse(), new Homography([1 / dmmPerPixel, 0, 0, 0, 1 / dmmPerPixel, 0, 0, 0, 1]));
            var rectified = PortableImaging.WarpPerspective(image, toRectified, w, h);
            var drawing = Rendering.SceneRasterizer.Rasterize(pages[0] with { Items = [.. words] }, dpi, words: true);
            double a = 0, b = 0, aa = 0, bb = 0, ab = 0;
            long n = 0;
            foreach (var word in words)
            {
                var points = Rendering.SheetGlyphs.Contours(word).SelectMany(c => c).ToList();
                if (points.Count == 0)
                {
                    continue;
                }

                // The box in drawing pixels, half-dmm to pixels, with a little room round it for the registration.
                double k = dpi / 508.0;
                int x0 = Math.Max(0, (int)(points.Min(p => p.X) * k) - 4), x1 = Math.Min(Math.Min(w, drawing.Width) - 1, (int)(points.Max(p => p.X) * k) + 4);
                int y0 = Math.Max(0, (int)(points.Min(p => p.Y) * k) - 4), y1 = Math.Min(Math.Min(h, drawing.Height) - 1, (int)(points.Max(p => p.Y) * k) + 4);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        double p = rectified.Pixels[(y * w) + x], q = drawing.Pixels[(y * drawing.Width) + x];
                        (a, b, aa, bb, ab, n) = (a + p, b + q, aa + (p * p), bb + (q * q), ab + (p * q), n + 1);
                    }
                }
            }

            double cov = ab - (a * b / Math.Max(1, n)), va = aa - (a * a / Math.Max(1, n)), vb = bb - (b * b / Math.Max(1, n));
            scored.Add((candidate, va > 0 && vb > 0 ? cov / Math.Sqrt(va * vb) : 0));
        }

        var ranked = scored.OrderByDescending(s => s.Correlation).ToList();
        return ranked.Count > 0 && ranked[0].Correlation >= NameAlike && (ranked.Count == 1 || ranked[0].Correlation - ranked[1].Correlation >= NameClearly)
            ? ranked[0].Definition
            : null;
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

    /// <summary>
    /// The markers, by id, and the code centres a sheet prints, in dmm: derived where the definition derives them (TARGET-SCHEMA.md sections
    /// 3.7 and 3.8), for the tile whose ids the found markers carry most of.
    /// </summary>
    public static (IReadOnlyList<Marker> Markers, IReadOnlyList<PointDmm> Codes) Printed(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var markers = PageRegistration.ExpectedMarkers(definition, 0);
        return (markers, CodeCentres(definition));
    }

    private static IReadOnlyList<PointDmm> CodeCentres(TargetDefinition definition) => definition.Codes is not { } c ? []
        : c.Placement == CodePlacement.Corners1
            ? Gltd.Derivation.Corners1.Positions(definition.Page.Width, definition.Page.Height, definition.DataBlock?.Height ?? 0, c.Count, c.ModuleSize)
            : c.Positions;

    private static (Dictionary<int, Marker> ById, IReadOnlyList<PointDmm> Codes) Printed(TargetDefinition definition, IReadOnlyList<DetectedMarker> found)
    {
        int tiles = definition.Tiling is { } t ? Math.Max(1, t.Cols * t.Rows) : 1;
        var ids = found.Select(m => m.Id).ToHashSet();
        var markers = Enumerable.Range(0, tiles).Select(i => PageRegistration.ExpectedMarkers(definition, i)).MaxBy(e => e.Count(m => ids.Contains(m.Id))) ?? [];
        var byId = markers.GroupBy(m => m.Id).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
        return (byId, CodeCentres(definition));
    }

    private static PointD Centre(DetectedMarker marker) =>
        new(marker.Corners.Average(c => c.X), marker.Corners.Average(c => c.Y));

    private static double Side(DetectedMarker marker) =>
        Enumerable.Range(0, marker.Corners.Count).Average(i => Math.Sqrt(Squared(marker.Corners[i], marker.Corners[(i + 1) % marker.Corners.Count])));
}
