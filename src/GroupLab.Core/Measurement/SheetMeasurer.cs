using System.Globalization;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Measurement;

/// <summary>The registration model of DETECTION-PIPELINE.md stage S3: a homography for scans, a lens model for photographs.</summary>
public enum RegistrationModel
{
    Auto,
    Homography,
    Radial,
}

/// <summary>
/// Settings for one measurement. Everything but the tile, resolution, locator and model exists for the measurements of
/// PHASE0-SPIKE-BRIEF.md section 6, and defaults to what the pipeline ships. The shipped locator is the edge fit, chosen
/// on the synthetic raster first as section 5 requires, where truth is known: 0.00022 in worst at 300 DPI and 0.00013 at
/// 600 against the centroid's 0.00069 and 0.00025.
/// </summary>
public sealed record MeasureOptions(
    int? TileIndex = null,
    double? Dpi = null,
    BullLocatorKind Locator = BullLocatorKind.EdgeFit,
    RegistrationModel Model = RegistrationModel.Auto,
    double? MaskRadius = null,
    CornerRefinement Refinement = CornerRefinement.Subpixel,
    double? RefinementWindowModules = null,
    int? ThresholdWindowMaxPixels = null,
    int DownsampleFactor = 1);

/// <summary>A decoded marker matched to the one printed, corners top-left first in both image pixels and page dmm.</summary>
public sealed record MarkerMatch(int Id, IReadOnlyList<PointD> ImageCorners, IReadOnlyList<PointD> PageCorners);

/// <summary>
/// Stage S2's outcome, with the scale it settled on in image pixels per page dmm, the printed markers it did not find,
/// and the candidate quads that did not decode, so stage S3 can say which of the two a missing marker was.
/// </summary>
public sealed record FiducialResult(
    int TileIndex,
    int Tiles,
    int Expected,
    IReadOnlyList<MarkerMatch> Matches,
    int Unexpected,
    int ShapeRejected,
    int CandidatesNotDecoded,
    double PixelsPerDmm,
    double MarkerSize,
    IReadOnlyList<Marker> Missing,
    IReadOnlyList<IReadOnlyList<PointD>> Undecoded);

/// <summary>One marker corner against the fitted registration, in page dmm.</summary>
public sealed record CornerResidual(int MarkerId, int Corner, PointD Image, PointD Page, double Error, bool Inlier);

/// <summary>
/// Stage S3's outcome. The residual is a diagnostic, reported as RMS with the maximum alongside and gating nothing,
/// DESIGN.md section 21. For a photograph the plain homography's RMS is kept too, to show what the lens term bought.
/// </summary>
public sealed record RegistrationFit(IPageMapping Mapping, IReadOnlyList<CornerResidual> Corners, double RmsResidual, double MaxResidual, double? HomographyRmsResidual)
{
    public int Markers => Corners.Select(c => c.MarkerId).Distinct().Count();

    public int Inliers => Corners.Count(c => c.Inlier);
}

/// <summary>Resolution measured by the fiducials at the page centre, and the print scale it implies where the file states a resolution.</summary>
public sealed record ScaleReport(double PixelsPerDmmX, double PixelsPerDmmY, double PixelsPerDmmArea, double? NominalDpi)
{
    public double? ScaleX => NominalDpi is { } d ? PixelsPerDmmX * 254 / d : null;

    public double? ScaleY => NominalDpi is { } d ? PixelsPerDmmY * 254 / d : null;

    public double? Scale => NominalDpi is { } d ? PixelsPerDmmArea * 254 / d : null;
}

/// <summary>
/// DESIGN.md section 11's photograph report: the fitted coefficients, the largest displacement the distortion term makes
/// at the frame edge in pixels, and that displacement at the target plane in inches, which is how wrong the measurement
/// would have been without the correction.
/// </summary>
public sealed record LensReport(double K1, double K2, double FrameEdgePixels, double FrameEdgeInches, double MarkerInches);

public sealed record SheetMeasurement(
    IReadOnlyList<StageRecord> Trace,
    FiducialResult? Fiducials,
    RegistrationFit? Registration,
    ScaleReport? Scale,
    LensReport? Lens,
    IReadOnlyList<BullLocation> Bulls,
    string? Failure)
{
    public BullLocation? WorstBull => Bulls.Where(b => b.Recovered is not null).MaxBy(b => b.Error);

    public double WorstError => WorstBull?.Error ?? double.NaN;

    public double MeanError => Bulls.Any(b => b.Recovered is not null) ? Bulls.Where(b => b.Recovered is not null).Average(b => b.Error) : double.NaN;
}

/// <summary>
/// The Phase 0 measurement harness of PHASE0-SPIKE-BRIEF.md section 4: stages S1 to S4 of DETECTION-PIPELINE.md and a
/// bull-location stage, and nothing more. Each stage is public so the measurements of section 6 can refit and relocate
/// without repeating detection.
/// </summary>
public static class SheetMeasurer
{
    /// <summary>
    /// RANSAC inlier distance for a photograph's first homography, in page dmm. Before the lens is modelled an ultra-wide
    /// puts true corners several dmm from any homography, so this pass rejects only misreads; the lens fit then reclassifies
    /// every corner at the scan threshold.
    /// </summary>
    public const double PhotographRansacThreshold = 12.7;

    public static SheetMeasurement Measure(GrayImage image, ImageMetadata metadata, TargetDefinition definition, MeasureOptions options, IImagingBackend backend, TraceRecorder? trace = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(backend);
        trace ??= new TraceRecorder();
        var fiducials = DetectFiducials(image, metadata, definition, options, backend, trace);
        if (fiducials.Matches.Count < 4)
        {
            return new SheetMeasurement(trace.Records, fiducials, null, null, null, [],
                string.Create(CultureInfo.InvariantCulture, $"{fiducials.Matches.Count} of {fiducials.Expected} markers found; registration needs 4"));
        }

        var fit = Register(image, metadata, fiducials, options, backend, trace);
        if (fit is null)
        {
            return new SheetMeasurement(trace.Records, fiducials, null, null, null, [], "registration failed");
        }

        var (scale, lens) = VerifyScale(metadata, definition, options, fit, image, trace);
        var bulls = LocateBulls(image, definition, fit.Mapping, options, trace);
        return new SheetMeasurement(trace.Records, fiducials, fit, scale, lens, bulls, null);
    }

    /// <summary>
    /// Stages S1 (provisional) and S2. Without a resolution to trust, DETECTION-PIPELINE.md section 3's bootstrap: a
    /// permissive pass sized from the image, then a pass sized from the markers it decoded. The tile is the one whose
    /// section 3.7 identifiers were decoded.
    /// </summary>
    public static FiducialResult DetectFiducials(GrayImage image, ImageMetadata metadata, TargetDefinition definition, MeasureOptions options, IImagingBackend backend, TraceRecorder trace)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(trace);
        var f = definition.Fiducials ?? throw new ArgumentException("The definition has no fiducials to register against.", nameof(definition));
        var inv = CultureInfo.InvariantCulture;
        double? dpi = options.Dpi ?? metadata.DpiX;
        using (var s1 = trace.Begin("S1.scale"))
        {
            if (dpi is { } stated)
            {
                string source = options.Dpi is not null ? "the command line" : $"{metadata.Format} metadata";
                s1.Decide("provisional scale", string.Create(inv, $"{stated:0.0} DPI"), $"{source} states it, and the fiducials check it after S3", "bootstrap from the markers");
                s1.Done(StageStatus.Ok, string.Create(inv, $"{stated:0.0} DPI from {source}; provisional"));
            }
            else
            {
                s1.Decide("provisional scale", "bootstrap from the markers",
                    metadata.IsCamera ? "a camera image carries no physical resolution (DESIGN.md section 11)" : "the file states no resolution", "an assumed resolution");
                s1.Done(StageStatus.Ok, metadata.IsCamera ? "camera image, no resolution to trust; the markers set it" : "no resolution in the file; the markers set it");
            }
        }

        using var s2 = trace.Begin("S2.fiducials");
        double pixelsPerDmm = dpi is { } known
            ? known / 254
            : 0.5 * Math.Max(image.Width, image.Height) / Math.Max(definition.Page.Width, definition.Page.Height);
        var detection = backend.DetectMarkers(image, DetectionOptions(f.MarkerSize * pixelsPerDmm, options));
        if (dpi is null)
        {
            double[] sides = [.. detection.Markers.Select(MeanSide).Order()];
            if (sides.Length > 0)
            {
                double side = sides[sides.Length / 2];
                s2.Decide("marker size", string.Create(inv, $"{side:0.0} px"),
                    string.Create(inv, $"it is the median side of {sides.Length} markers decoded by a first pass sized for {f.MarkerSize * pixelsPerDmm:0.0} px"),
                    "the first pass alone");
                pixelsPerDmm = side / f.MarkerSize;
                detection = backend.DetectMarkers(image, DetectionOptions(side, options));
            }
        }

        int tiles = definition.Tiling is { } tiling ? tiling.Cols * tiling.Rows : 1;
        var decoded = detection.Markers.Select(m => m.Id).ToHashSet();
        var expectedByTile = Enumerable.Range(0, tiles).Select(i => PageRegistration.ExpectedMarkers(definition, i)).ToArray();
        int[] hits = [.. expectedByTile.Select(e => e.Count(m => decoded.Contains(m.Id)))];
        int tile = options.TileIndex ?? Array.IndexOf(hits, hits.Max());
        if (tiles > 1)
        {
            string counts = string.Join(", ", hits.Select((h, i) => string.Create(inv, $"tile {i + 1} {h}")));
            s2.Decide("tile", string.Create(inv, $"{tile + 1} of {tiles}"),
                options.TileIndex is not null ? "the command line names it" : $"its markers are the ones decoded, and TARGET-SCHEMA.md section 3.7 ids are unique across the assembly ({counts})",
                [.. Enumerable.Range(0, tiles).Where(i => i != tile).Select(i => string.Create(inv, $"tile {i + 1}"))]);
        }

        var printed = expectedByTile[tile];
        var unmatched = printed.GroupBy(m => m.Id).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
        var matches = new List<MarkerMatch>();
        int unexpected = 0;
        double half = f.MarkerSize / 2.0;
        foreach (var marker in detection.Markers)
        {
            if (!unmatched.Remove(marker.Id, out var m))
            {
                unexpected++;
                s2.Reject(string.Create(inv, $"marker {marker.Id}"), "decoded, but not printed on this tile or already matched");
                continue;
            }

            matches.Add(new MarkerMatch(marker.Id, marker.Corners,
                [new(m.X - half, m.Y - half), new(m.X + half, m.Y - half), new(m.X + half, m.Y + half), new(m.X - half, m.Y + half)]));
        }

        foreach (var missing in unmatched.Values.OrderBy(m => m.Id))
        {
            s2.Reject(string.Create(inv, $"marker {missing.Id}"), "printed, not found", PointInches.FromDmm(missing.X, missing.Y));
        }

        foreach (var rejected in detection.Rejected)
        {
            s2.Reject(string.Create(inv, $"candidate {rejected.Id}"), rejected.Reason);
        }

        var used = DetectionOptions(f.MarkerSize * pixelsPerDmm, options);
        s2.Parameter("markerSide", string.Create(inv, $"{f.MarkerSize / 10.0:0.0} mm = {used.ExpectedMarkerSidePixels:0.0} px"));
        s2.Parameter("refinement", string.Create(inv, $"{used.Refinement}, window {(used.RefinementWindowModules is { } w ? $"{w:0.##} modules" : "OpenCvSharpBackend default")}"));
        s2.Parameter("thresholdWindowMax", used.ThresholdWindowMaxPixels is { } t ? string.Create(inv, $"{t} px") : "sized from the marker");
        s2.Parameter("downsample", string.Create(inv, $"{used.DownsampleFactor}x"));
        s2.Metric("sought", printed.Count, "markers");
        s2.Metric("matched", matches.Count, "markers");
        s2.Metric("unexpected", unexpected, "markers");
        s2.Metric("shapeRejected", detection.Rejected.Count, "candidates");
        s2.Metric("notDecoded", detection.CandidatesNotDecoded, "candidates");

        int notFound = printed.Count - matches.Count;
        string summary = string.Create(inv, $"{printed.Count} markers sought, {matches.Count} decoded, {notFound} not found")
            + (unexpected > 0 ? string.Create(inv, $", {unexpected} unexpected") : "");
        s2.Done(matches.Count < 4 ? StageStatus.Failed : notFound > 0 ? StageStatus.Degraded : StageStatus.Ok, summary);
        return new FiducialResult(tile, tiles, printed.Count, matches, unexpected, detection.Rejected.Count, detection.CandidatesNotDecoded, pixelsPerDmm,
            f.MarkerSize, [.. unmatched.Values.OrderBy(m => m.Id)], detection.Undecoded);
    }

    /// <summary>
    /// Stage S3: RANSAC against the known page coordinates, then solve (DETECTION-PIPELINE.md). A homography for a scan; for
    /// a photograph, DESIGN.md section 11 step 3's lens model fitted to the homography's inliers, with every corner then
    /// reclassified against it. <paramref name="markerSubset"/> refits on some markers only, for measurement 1.
    /// </summary>
    public static RegistrationFit? Register(GrayImage image, ImageMetadata metadata, FiducialResult fiducials, MeasureOptions options, IImagingBackend backend, TraceRecorder trace, IReadOnlyCollection<int>? markerSubset = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(fiducials);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(trace);
        var inv = CultureInfo.InvariantCulture;
        using var stage = trace.Begin("S3.register");
        var matches = markerSubset is null ? fiducials.Matches : fiducials.Matches.Where(m => markerSubset.Contains(m.Id)).ToList();
        var model = options.Model != RegistrationModel.Auto ? options.Model
            : metadata.IsCamera ? RegistrationModel.Radial : RegistrationModel.Homography;
        stage.Decide("model", model == RegistrationModel.Radial ? "homography with radial distortion" : "homography",
            options.Model != RegistrationModel.Auto ? "the command line names it"
            : metadata.IsCamera ? "a camera image, whose lens distorts radially (DESIGN.md section 11)"
            : "a flatbed scan (DETECTION-PIPELINE.md stage S3)",
            model == RegistrationModel.Radial ? "homography" : "homography with radial distortion");
        if (matches.Count < 4)
        {
            stage.Done(StageStatus.Failed, string.Create(inv, $"{matches.Count} markers; a homography needs 4"));
            return null;
        }

        var imagePoints = matches.SelectMany(m => m.ImageCorners).ToList();
        var pagePoints = matches.SelectMany(m => m.PageCorners).ToList();
        double threshold = model == RegistrationModel.Radial ? PhotographRansacThreshold : PageRegistration.RansacThreshold;
        stage.Parameter("ransacThreshold", string.Create(inv, $"{threshold:0.00} dmm = {threshold * fiducials.PixelsPerDmm:0.0} px"));

        HomographyFit homography;
        try
        {
            homography = backend.FindHomography(imagePoints, pagePoints, threshold);
        }
        catch (InvalidOperationException ex)
        {
            stage.Done(StageStatus.Failed, ex.Message);
            return null;
        }

        IPageMapping mapping = new HomographyMapping(homography.Transform);
        bool[] inliers = [.. homography.Inliers];
        double? homographyRms = null;
        if (model == RegistrationModel.Radial)
        {
            homographyRms = Rms(mapping, imagePoints, pagePoints, inliers);
            try
            {
                var lens = LensFit.Fit(Select(imagePoints, inliers), Select(pagePoints, inliers), homography.Transform, image.Width, image.Height);
                inliers = [.. imagePoints.Select((p, i) => Distance(lens.ToPage(p), pagePoints[i]) <= PageRegistration.RansacThreshold)];
                mapping = LensFit.Fit(Select(imagePoints, inliers), Select(pagePoints, inliers), homography.Transform, image.Width, image.Height);
            }
            catch (InvalidOperationException ex)
            {
                stage.Done(StageStatus.Failed, ex.Message);
                return null;
            }
        }

        var corners = new List<CornerResidual>(imagePoints.Count);
        double sum = 0, max = 0;
        for (int i = 0; i < imagePoints.Count; i++)
        {
            var page = pagePoints[i];
            double error = Distance(mapping.ToPage(imagePoints[i]), page);
            int id = matches[i / 4].Id;
            corners.Add(new CornerResidual(id, i % 4, imagePoints[i], page, error, inliers[i]));
            if (inliers[i])
            {
                sum += error * error;
                max = Math.Max(max, error);
            }
            else
            {
                stage.Reject(string.Create(inv, $"marker {id} corner {i % 4}"), string.Create(inv, $"{error:0.00} dmm from the fit"), PointInches.FromDmm(page.X, page.Y));
            }
        }

        // With the page registered, each marker S2 did not find can be looked for where it should be: a candidate quad
        // there that failed to decode is a reading failure, and no quad at all is a finding failure.
        int undecodedHere = 0;
        foreach (var missing in fiducials.Missing)
        {
            var expected = mapping.ToImage(new PointD(missing.X, missing.Y));
            double reach = fiducials.MarkerSize * fiducials.PixelsPerDmm / 2;
            bool quad = fiducials.Undecoded.Any(q => Distance(new PointD(q.Average(p => p.X), q.Average(p => p.Y)), expected) < reach);
            undecodedHere += quad ? 1 : 0;
            stage.Reject(string.Create(inv, $"marker {missing.Id}"),
                quad ? "not found: a candidate quad at its position did not decode" : "not found: no candidate quad at its position",
                PointInches.FromDmm(missing.X, missing.Y));
        }

        if (fiducials.Missing.Count > 0)
        {
            stage.Metric("missingFoundButUnread", undecodedHere, "markers");
            stage.Metric("missingNeverFound", fiducials.Missing.Count - undecodedHere, "markers");
        }

        int count = inliers.Count(x => x);
        double rms = Math.Sqrt(sum / count);
        stage.Metric("markers", matches.Count, "markers");
        stage.Metric("inliers", count, "corners");
        stage.Metric("residualRms", rms / 254, "in");
        stage.Metric("residualMax", max / 254, "in");
        if (homographyRms is { } h)
        {
            stage.Metric("homographyResidualRms", h / 254, "in");
        }

        stage.Detail(string.Create(inv, $"residual rms {rms / 254:0.00000} in, max {max / 254:0.00000} in")
            + (homographyRms is { } hr ? string.Create(inv, $"; homography alone {hr / 254:0.00000} in rms") : ""));
        stage.Done(StageStatus.Ok, string.Create(inv, $"{mapping.Model}, {matches.Count} markers, {count} of {imagePoints.Count} corners inliers"));
        return new RegistrationFit(mapping, corners, rms, max, homographyRms);
    }

    /// <summary>
    /// Stage S1 again from the fiducials, and stage S4: resolution by axis and by area at the page centre, the print scale
    /// against a stated resolution, and for a photograph the lens report of DESIGN.md section 11.
    /// </summary>
    public static (ScaleReport Scale, LensReport? Lens) VerifyScale(ImageMetadata metadata, TargetDefinition definition, MeasureOptions options, RegistrationFit fit, GrayImage image, TraceRecorder trace)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fit);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(trace);
        var inv = CultureInfo.InvariantCulture;
        var mapping = fit.Mapping;
        var j = mapping.Jacobian(mapping.ToImage(new PointD(definition.Page.Width / 2.0, definition.Page.Height / 2.0)));
        double det = Math.Abs((j.XX * j.YY) - (j.XY * j.YX));
        var scale = new ScaleReport(
            Math.Sqrt((j.YY * j.YY) + (j.YX * j.YX)) / det,
            Math.Sqrt((j.XY * j.XY) + (j.XX * j.XX)) / det,
            1 / Math.Sqrt(det),
            options.Dpi ?? metadata.DpiX);

        using (var s1 = trace.Begin("S1.scale"))
        {
            s1.Metric("resolutionX", scale.PixelsPerDmmX * 254, "DPI");
            s1.Metric("resolutionY", scale.PixelsPerDmmY * 254, "DPI");
            s1.Done(StageStatus.Ok, string.Create(inv, $"{scale.PixelsPerDmmX * 254:0.0} DPI x, {scale.PixelsPerDmmY * 254:0.0} DPI y from fiducials at the page centre")
                + (scale.NominalDpi is { } n ? string.Create(inv, $"; the file stated {n:0.0}") : ""));
        }

        using var s4 = trace.Begin("S4.verify");
        LensReport? lens = null;
        if (mapping is RadialHomographyMapping radial)
        {
            double edgePixels = 0, edgeInches = 0, markerInches = 0;
            foreach (var corner in (PointD[])[new(0, 0), new(image.Width - 1, 0), new(image.Width - 1, image.Height - 1), new(0, image.Height - 1)])
            {
                double ux = (corner.X - radial.CentreX) / radial.Scale, uy = (corner.Y - radial.CentreY) / radial.Scale;
                var undistorted = radial.Undistort(corner);
                edgePixels = Math.Max(edgePixels, radial.Scale * Math.Sqrt(Math.Pow(undistorted.X - ux, 2) + Math.Pow(undistorted.Y - uy, 2)));
                edgeInches = Math.Max(edgeInches, Distance(radial.ToPage(corner), radial.WithoutDistortion(corner)) / 254);
            }

            foreach (var c in fit.Corners)
            {
                markerInches = Math.Max(markerInches, Distance(radial.ToPage(c.Image), radial.WithoutDistortion(c.Image)) / 254);
            }

            lens = new LensReport(radial.K1, radial.K2, edgePixels, edgeInches, markerInches);
            s4.Metric("k1", radial.K1, "");
            s4.Metric("k2", radial.K2, "");
            s4.Metric("frameEdgeDistortion", edgePixels, "px");
            s4.Metric("frameEdgeDistortionAtTarget", edgeInches, "in");
            s4.Metric("markerDistortionAtTarget", markerInches, "in");
            s4.Detail(string.Create(inv, $"lens k1 {radial.K1:+0.00000;-0.00000}, k2 {radial.K2:+0.00000;-0.00000}"));
            s4.Detail(string.Create(inv, $"distortion at the frame edge {edgePixels:0.0} px = {edgeInches:0.0000} in at the target plane; {markerInches:0.0000} in at the outermost marker"));
        }

        if (scale.Scale is { } area)
        {
            s4.Metric("printScale", area, "ratio");
            s4.Metric("printScaleX", scale.ScaleX!.Value, "ratio");
            s4.Metric("printScaleY", scale.ScaleY!.Value, "ratio");
            s4.Done(StageStatus.Ok, string.Create(inv, $"printed at {area * 100:0.00}% of intended size, x {scale.ScaleX * 100:0.00}%, y {scale.ScaleY * 100:0.00}%"));
        }
        else
        {
            s4.Done(StageStatus.Ok, string.Create(inv, $"photograph, no stated resolution; {scale.PixelsPerDmmArea * 254:0} px per inch at the page centre"));
        }

        return (scale, lens);
    }

    /// <summary>The Phase 0 bull stage: every bull on the sheet, scoring and sighter, by the locator <paramref name="options"/> names.</summary>
    public static IReadOnlyList<BullLocation> LocateBulls(GrayImage image, TargetDefinition definition, IPageMapping mapping, MeasureOptions options, TraceRecorder trace)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(trace);
        var inv = CultureInfo.InvariantCulture;
        using var stage = trace.Begin("P0.bulls");
        var sets = definition.RingSets.GroupBy(s => s.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var j = mapping.Jacobian(mapping.ToImage(new PointD(definition.Page.Width / 2.0, definition.Page.Height / 2.0)));
        double pixelsPerDmm = 1 / Math.Sqrt(Math.Abs((j.XX * j.YY) - (j.XY * j.YX)));
        stage.Parameter("locator", options.Locator == BullLocatorKind.Centroid ? "thresholded ink-weighted centroid" : "edge fit to the declared disc radii");
        var reported = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<BullLocation>(definition.Bulls.Count);
        for (int i = 0; i < definition.Bulls.Count; i++)
        {
            var bull = definition.Bulls[i];
            if (!sets.TryGetValue(bull.RingSet, out var set))
            {
                results.Add(new BullLocation(i, bull.Label, new PointD(bull.X, bull.Y), null, 0, Failure: "its ring set is not defined"));
                continue;
            }

            var bands = RingGeometry.Bands(definition, set);
            BullLocation located;
            if (options.Locator == BullLocatorKind.Centroid)
            {
                double radius = options.MaskRadius ?? RingGeometry.MaskRadius(bands);
                if (reported.Add(set.Key))
                {
                    stage.Parameter($"maskRadius[{set.Key}]", string.Create(inv, $"{radius:0.00} dmm = {radius * pixelsPerDmm:0.0} px"));
                }

                located = CentroidBullLocator.Locate(image, mapping, i, bull, radius);
            }
            else
            {
                if (reported.Add(set.Key))
                {
                    stage.Parameter($"edges[{set.Key}]", string.Create(inv,
                        $"{bands.Sum(b => b.Inner > 0 ? 2 : 1)} edges, {EdgeFitBullLocator.Rays} rays, up to {EdgeFitBullLocator.MaximumHalfWidth:0.0} dmm = {EdgeFitBullLocator.MaximumHalfWidth * pixelsPerDmm:0.0} px each side"));
                }

                located = EdgeFitBullLocator.Locate(image, mapping, i, bull, bands);
            }

            if (located.Failure is { } why)
            {
                stage.Reject($"bull {located.Name}", why, PointInches.FromDmm(bull.X, bull.Y));
            }

            results.Add(located);
        }

        var found = results.Where(r => r.Recovered is not null).ToList();
        if (found.Count == 0)
        {
            stage.Done(StageStatus.Failed, "no bull located");
            return results;
        }

        var worst = found.MaxBy(r => r.Error)!;
        double mean = found.Average(r => r.Error);
        stage.Metric("located", found.Count, "bulls");
        stage.Metric("meanError", mean / 254, "in");
        stage.Metric("worstError", worst.Error / 254, "in");
        if (found.All(r => r.InkSpread is not null))
        {
            stage.Metric("inkSpread", found.Average(r => r.InkSpread!.Value) / 10, "mm per edge");
        }

        stage.Done(results.All(r => r.Failure is null) ? StageStatus.Ok : StageStatus.Degraded,
            string.Create(inv, $"{found.Count} of {results.Count} bulls located, mean {mean / 254:0.00000} in, worst {worst.Error / 254:0.00000} in at {worst.Name}"));
        return results;
    }

    private static MarkerDetectionOptions DetectionOptions(double sidePixels, MeasureOptions o) =>
        new(MarkerFamily.AprilTag36h11, sidePixels, o.Refinement, o.RefinementWindowModules, o.ThresholdWindowMaxPixels, o.DownsampleFactor);

    private static double MeanSide(DetectedMarker marker)
    {
        double sum = 0;
        for (int k = 0; k < 4; k++)
        {
            sum += Distance(marker.Corners[k], marker.Corners[(k + 1) % 4]);
        }

        return sum / 4;
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private static List<PointD> Select(List<PointD> points, bool[] keep) => [.. points.Where((_, i) => keep[i])];

    private static double Rms(IPageMapping mapping, List<PointD> image, List<PointD> page, bool[] keep)
    {
        double sum = 0;
        int n = 0;
        for (int i = 0; i < image.Count; i++)
        {
            if (keep[i])
            {
                sum += Math.Pow(Distance(mapping.ToPage(image[i]), page[i]), 2);
                n++;
            }
        }

        return Math.Sqrt(sum / n);
    }
}
