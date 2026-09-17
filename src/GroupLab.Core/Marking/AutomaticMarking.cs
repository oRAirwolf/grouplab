using System.Globalization;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Marking;

/// <summary>
/// What the automatic path hands the marking screen: the measurement with its trace, the registered scale, the bulls, the detected
/// shots with their assignment, a one-line summary, the failure when there is one, and the sheet's printed artwork in image pixels, which
/// the screen's snap and size check read to tell printed ink from a hole (NOTES-FROM-PLANNING.md entry 40 section 1). Each detection
/// keeps its whole assignment, margin and nearest bull included, the matching's method and reason travel with them, and the candidates
/// the detector refused come through too, so the editor has what it shows (entry 70 section 4).
/// </summary>
public sealed record AutomaticResult(
    SheetMeasurement Measurement,
    SheetReference? Scale,
    IReadOnlyList<BullAim> Bulls,
    IReadOnlyList<DetectedShot> Detections,
    IReadOnlyList<PointD> MissingMarkers,
    string Summary,
    string? Failure,
    GrayImage? ExpectedArtwork = null,
    ShotAssignmentResult? Assignment = null,
    IReadOnlyList<RejectedCandidate>? Rejected = null,
    RenderDifferenceResult? Difference = null,
    DetectionRecord? Detection = null);

/// <summary>
/// The automatic path for a GroupLab sheet, as NOTES-FROM-PLANNING.md entry 21 section 3 frames it: a way of pre-filling the marks
/// the user then accepts, moves or deletes. Registration from the printed markers (stages S1 to S4), render-and-difference hole
/// detection (S5 to S8, M2.2), and assignment by one-to-one matching where the counts allow it (S9). Every result is a set of
/// ordinary marks, so nothing the pipeline decides is final.
/// </summary>
public static class AutomaticMarking
{
    /// <summary>
    /// What render-and-difference measures a hole at, as a fraction of the stated bullet diameter, NOTES-FROM-PLANNING.md entry 79 section 1:
    /// the bullet is not the hole. Measured on the two real .308 sheets, whole detections matched to hand-verified holes. On scans there are
    /// only two sheets, and the sheet is the unit of uncertainty (entry 80 section 3): 0.952 over 13 holes and 0.934 over 11, pooled 0.944.
    /// The holes within a sheet share paper, printer, scanner and bullet, so their spread of 0.039 says little about the next sheet. On
    /// photographs, 75 holes over seven frames of the same two sheets: pooled 0.986, frame means from 0.92 to 1.07. The detector's extent is the torn crown, which reaches about the calibre, not the bright aperture of
    /// about 0.68 of it. Only the split's veto reads it, a separation between one hole and two, never an absolute size.
    /// <para>
    /// What these figures are good for, entry 81 section 4: telling one hole from two is a factor-of-two judgement, and an error of a few
    /// percent in them cannot flip it, so two sheets are enough for that. They are not enough for anything that needs the absolute size, such
    /// as reporting a measured calibre back to a person or comparing hole sizes between loads. A use of that kind must measure the ratio on
    /// many more sheets first rather than inherit a precision these never had.
    /// </para>
    /// </summary>
    public const double ScanHoleToCalibre = 0.944, PhotographHoleToCalibre = 0.986;

    /// <param name="grey">The image as grey, for the markers.</param>
    /// <param name="value">The image as HSV value, max(R, G, B), for the holes.</param>
    /// <param name="trace">
    /// Where each stage records what it did (DESIGN.md section 19 [r3]); registration's stages come from <see cref="SheetMeasurer"/>,
    /// and the hole detection and assignment stages are recorded here.
    /// </param>
    /// <param name="cancellation">
    /// Checked between the stages, so a screen can stop a detection it started on its own (NOTES-FROM-PLANNING.md entry 76 section 4). A stage
    /// already running finishes first.
    /// </param>
    /// <param name="calibre">
    /// The bullet, when the person has named one: segmentation reads it so a blob too small to be two holes is never split in two
    /// (NOTES-FROM-PLANNING.md entry 78 section 4). Optional, and detection without it is what it always was.
    /// </param>
    public static AutomaticResult Run(GrayImage grey, GrayImage value, ImageMetadata metadata, TargetDefinition definition, IImagingBackend backend, Trace.TraceRecorder? trace = null, CancellationToken cancellation = default, Calibre? calibre = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellation.ThrowIfCancellationRequested();
        trace ??= new Trace.TraceRecorder();
        var measurement = SheetMeasurer.Measure(grey, metadata, definition, new MeasureOptions(), backend, trace);
        cancellation.ThrowIfCancellationRequested();
        var fiducials = measurement.Fiducials;
        string markers = fiducials is null ? "no markers" : string.Create(CultureInfo.InvariantCulture, $"{fiducials.Matches.Count} of {fiducials.Expected} markers found");
        if (measurement.Registration is not { } registration || fiducials is null)
        {
            return new AutomaticResult(measurement, null, [], [], [], markers, measurement.Failure ?? "registration failed");
        }

        var mapping = registration.Mapping;
        var missing = fiducials.Missing.Select(m => mapping.ToImage(new PointD(m.X, m.Y))).ToList();
        double dpi = (measurement.Scale?.PixelsPerDmmArea ?? fiducials.PixelsPerDmm) * 254;
        RenderDifferenceResult holes;
        DetectionRecord detection;
        using (var stage = trace.Begin("S5-S8.holes"))
        {
            try
            {
                double ratio = metadata.IsCamera ? PhotographHoleToCalibre : ScanHoleToCalibre;
                detection = new DetectionRecord(calibre, calibre?.DiameterInches * ratio);
                holes = RenderDifferenceHoleDetector.Detect(value, definition, fiducials.TileIndex, mapping, dpi, backend, new RenderDifferenceOptions(CalibreInches: calibre?.DiameterInches * ratio));
            }
            catch (InvalidOperationException ex)
            {
                // DESIGN.md section 19: the trace is never the only place an error appears, so the failure is returned as well as recorded.
                stage.Done(StageStatus.Failed, ex.Message);
                return new AutomaticResult(measurement, null, [], [], missing, markers, "hole detection failed: " + ex.Message);
            }

            if (GroupLab.Core.Gltd.Validation.GltdValidator.Validate(definition).Any(d => d.Severity == GroupLab.Core.Gltd.Severity.Error))
            {
                stage.Decide("expected artwork", "rendered although the definition fails validation", "the sheet is already printed, and what was printed is what must be differenced", "refuse the sheet");
            }

            stage.Parameter("resolution", string.Create(CultureInfo.InvariantCulture, $"{dpi:0.0} px per inch, from the registration"));
            stage.Parameter("calibre", calibre is null ? "none named, so a single hole is the sheet's own 25th percentile mark once it has five, and shape alone decides before that" : string.Create(CultureInfo.InvariantCulture,
                $"{calibre.Name}, {calibre.DiameterInches:0.000} in, a hole of about {calibre.DiameterInches * (metadata.IsCamera ? PhotographHoleToCalibre : ScanHoleToCalibre):0.000} in {(metadata.IsCamera ? "in a photograph" : "on a scan")}: a blob under {new RenderDifferenceOptions().SplitMinimumHoles:0.0} such holes is not split"));
            stage.Metric("ink fraction", holes.InkFraction, "of paper");
            stage.Metric("holes", holes.Holes.Count, "count");
            stage.Metric("rejected", holes.Rejected.Count, "count");
            var swallowed = holes.InsideZones;
            stage.Metric("inside exclusion zones", swallowed.Count, "count");
            foreach (var zone in swallowed.GroupBy(r => r.Zone!).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                stage.Detail(string.Create(CultureInfo.InvariantCulture, $"{zone.Count()} hole-sized candidate{(zone.Count() == 1 ? "" : "s")} inside {zone.Key}, where no hole is looked for"));
            }

            foreach (var r in holes.Rejected)
            {
                var page = mapping.ToPage(new PointD(r.X, r.Y));
                stage.Reject(string.Create(CultureInfo.InvariantCulture, $"blob {r.DiameterInches:0.000} in across"), r.Reason, Trace.PointInches.FromDmm(page.X, page.Y));
            }

            int merges = holes.Holes.Count(h => h.PossibleMerge), oversized = holes.Holes.Count(h => h.Oversized), vetoed = holes.Holes.Count(h => h.SplitVetoed);
            int residue = holes.Rejected.Count(r => r.Reason.StartsWith("residue", StringComparison.Ordinal));
            stage.Metric("splits the hole size stopped", vetoed, "count");
            stage.Metric("residue refused", residue, "count");
            stage.Metric("split halves", merges, "count");
            stage.Metric("oversized", oversized, "count");
            stage.Done(StageStatus.Ok, string.Create(CultureInfo.InvariantCulture,
                $"{holes.Holes.Count} holes inside the registered sheet, {holes.Rejected.Count} candidates rejected, {swallowed.Count} of them hole-sized inside exclusion zones{(merges > 0 ? $", {merges} from split merges" : "")}{(oversized > 0 ? $", {oversized} oversized" : "")}{(vetoed > 0 ? $", {vetoed} kept whole by their size" : "")}{(residue > 0 ? $", {residue} refused as residue" : "")}"));
        }

        // Entry 70 section 5: two nearly identical positions, kept apart on purpose. A shot's offset is a measurement, and the shooter aimed
        // at the bull as printed, so the aim point is the located centre. Assignment is a classification, and it uses the definition's
        // exact geometry: registration error is of order 0.005 in and printing 0.003 in, against a 0.15 in ambiguity margin, so the choice
        // cannot flip an assignment that was not already flagged. Do not unify them for tidiness.
        var bulls = measurement.Bulls.Select(b => new BullAim(b.Index, b.Name, mapping.ToImage(b.Recovered ?? b.Declared), definition.Bulls[b.Index].Scoring, b.Declared)).ToList();
        var bullPages = definition.Bulls.Select(b => new PointD(b.X, b.Y)).ToList();
        var shotPages = holes.Holes.Select(h => mapping.ToPage(new PointD(h.X, h.Y))).ToList();
        ShotAssignmentResult assignment;
        cancellation.ThrowIfCancellationRequested();
        using (var stage = trace.Begin("S9.assign"))
        {
            // Entry 73 section 1: sighter and scoring bulls are matched as separate pools, so a sighter's hole never lands on a scoring bull.
            assignment = ShotAssignment.Assign(shotPages, bullPages, scoring: [.. definition.Bulls.Select(b => b.Scoring)]);
            int ambiguous = assignment.Shots.Count(s => s.Ambiguous), unassigned = assignment.Shots.Count(s => s.Bull is null);
            stage.Decide("assignment", assignment.Method.ToString(), assignment.Reason);
            foreach (var s in assignment.Shots.Where(s => s.Ambiguous))
            {
                stage.Detail(string.Create(CultureInfo.InvariantCulture,
                    $"shot {s.Shot + 1} ambiguous: given bull {s.Bull?.ToString(CultureInfo.InvariantCulture) ?? "none"} at {s.Distance / 254:0.000} in, nearest bull {s.NearestBull} at {s.NearestDistance / 254:0.000} in, margin {s.Margin / 254:0.000} in"));
            }

            stage.Done(ambiguous > 0 || unassigned > 0 ? StageStatus.Degraded : StageStatus.Ok, string.Create(CultureInfo.InvariantCulture,
                $"{assignment.Shots.Count} shots to {bullPages.Count} bulls by {assignment.Method}{(ambiguous > 0 ? $", {ambiguous} ambiguous" : "")}{(unassigned > 0 ? $", {unassigned} unassigned" : "")}"));
        }

        var detections = holes.Holes.Select((h, i) => new DetectedShot(new PointD(h.X, h.Y), assignment.Shots[i], h.DiameterInches)).ToList();
        var rejected = holes.Rejected.Select(r => new RejectedCandidate(new PointD(r.X, r.Y), r.DiameterInches, r.Reason)).ToList();

        string summary = string.Create(CultureInfo.InvariantCulture,
            $"{markers}, {detection.Describe()}, registration RMS {registration.RmsResidual / 254:0.0000} in over {registration.Markers} markers, {holes.Holes.Count} holes detected{(holes.InsideZones.Count > 0 ? $", {holes.InsideZones.Count} hole-sized candidate{(holes.InsideZones.Count == 1 ? "" : "s")} inside printed-matter zones not looked at" : "")}, assigned by {assignment.Method}: {assignment.Reason}");
        return new AutomaticResult(measurement, new SheetReference(mapping, summary), bulls, detections, missing, summary, null, holes.Expected, assignment, rejected, holes, detection);
    }
}

/// <summary>Where a tap was placed, and why it was not snapped to the dark under it when it was not.</summary>
public sealed record SnapResult(PointD At, string? NotSnapped);

/// <summary>
/// DESIGN.md section 13: rough clicks snap to the local centroid, so manual placement is not limited by mouse or finger precision.
/// A hole is darker than the paper around it, so the snap is the darkness-weighted centroid of the pixels within the radius that
/// are clearly darker than the window's paper level, taken twice, the second time about the first.
/// </summary>
public static class Snapping
{
    /// <summary>A pixel of the expected artwork at or above this level is paper, the detector's own reading of its render.</summary>
    public const byte ArtworkPaper = 230;

    public static PointD ToDarkCentroid(GrayImage value, PointD click, double radiusPixels) => Centroid(value, click, radiusPixels, null);

    /// <summary>
    /// The snap on a sheet whose printed artwork is known, NOTES-FROM-PLANNING.md entry 40 section 1: taps on a rendered, unshot sheet
    /// snapped to bull 13's inner ring, because a printed ring is darker than paper. The expected artwork says which dark pixels were
    /// printed. When most of the dark weight under the tap is printed, the tap is placed where it was made and
    /// <see cref="SnapResult.NotSnapped"/> says why; otherwise only the dark pixels the artwork calls paper pull the snap. Without
    /// artwork this is <see cref="ToDarkCentroid"/>.
    /// </summary>
    public static SnapResult ToHole(GrayImage value, PointD click, double radiusPixels, GrayImage? artwork)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (artwork is null || artwork.Width != value.Width || artwork.Height != value.Height)
        {
            return new SnapResult(ToDarkCentroid(value, click, radiusPixels), null);
        }

        var (onPaper, onInk) = DarkWeight(value, artwork, click, radiusPixels);
        if (onInk > 0 && onInk >= onPaper)
        {
            return new SnapResult(click, "placed where tapped: the dark area under the tap is the printed target, not a hole");
        }

        return new SnapResult(Centroid(value, click, radiusPixels, artwork), null);
    }

    /// <summary>The dark weight within the radius, as the snap weighs it, on pixels the artwork calls paper and on pixels it calls printed.</summary>
    private static (double OnPaper, double OnInk) DarkWeight(GrayImage value, GrayImage artwork, PointD click, double radiusPixels)
    {
        int x0 = Math.Max(0, (int)(click.X - radiusPixels)), x1 = Math.Min(value.Width - 1, (int)(click.X + radiusPixels));
        int y0 = Math.Max(0, (int)(click.Y - radiusPixels)), y1 = Math.Min(value.Height - 1, (int)(click.Y + radiusPixels));
        if (x1 <= x0 || y1 <= y0)
        {
            return (0, 0);
        }

        int paper = PaperLevel(value, x0, x1, y0, y1);
        double onPaper = 0, onInk = 0;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (((x - click.X) * (x - click.X)) + ((y - click.Y) * (y - click.Y)) > radiusPixels * radiusPixels)
                {
                    continue;
                }

                int i = (y * value.Width) + x;
                double w = paper - value.Pixels[i] - 40;
                if (w > 0)
                {
                    if (artwork.Pixels[i] >= ArtworkPaper)
                    {
                        onPaper += w;
                    }
                    else
                    {
                        onInk += w;
                    }
                }
            }
        }

        return (onPaper, onInk);
    }

    /// <summary>The window's paper level: the grey level nine tenths of its pixels are at or below.</summary>
    private static int PaperLevel(GrayImage value, int x0, int x1, int y0, int y1)
    {
        var histogram = new int[256];
        int count = 0;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                histogram[value.Pixels[(y * value.Width) + x]]++;
                count++;
            }
        }

        for (int level = 0, seen = 0; level < 256; level++)
        {
            seen += histogram[level];
            if (seen >= 0.9 * count)
            {
                return level;
            }
        }

        return 255;
    }

    private static PointD Centroid(GrayImage value, PointD click, double radiusPixels, GrayImage? artwork)
    {
        ArgumentNullException.ThrowIfNull(value);
        var centre = click;
        for (int pass = 0; pass < 2; pass++)
        {
            int x0 = Math.Max(0, (int)(centre.X - radiusPixels)), x1 = Math.Min(value.Width - 1, (int)(centre.X + radiusPixels));
            int y0 = Math.Max(0, (int)(centre.Y - radiusPixels)), y1 = Math.Min(value.Height - 1, (int)(centre.Y + radiusPixels));
            if (x1 <= x0 || y1 <= y0)
            {
                return click;
            }

            int paper = PaperLevel(value, x0, x1, y0, y1);
            double sw = 0, sx = 0, sy = 0;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (((x - centre.X) * (x - centre.X)) + ((y - centre.Y) * (y - centre.Y)) > radiusPixels * radiusPixels)
                    {
                        continue;
                    }

                    int i = (y * value.Width) + x;
                    double w = paper - value.Pixels[i] - 40;
                    if (w > 0 && (artwork is null || artwork.Pixels[i] >= ArtworkPaper))
                    {
                        sw += w;
                        sx += w * x;
                        sy += w * y;
                    }
                }
            }

            if (sw <= 0)
            {
                return click;
            }

            centre = new PointD(sx / sw, sy / sw);
        }

        return centre;
    }
}
