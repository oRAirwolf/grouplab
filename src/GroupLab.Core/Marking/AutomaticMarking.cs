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
/// the detector refused come through too, so the editor has what it shows (entry 70 section 4). The definition comes with a result that
/// registered, so the analysis state can draw one bull's artwork behind the composite plot (entry 103 section 1).
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
    DetectionRecord? Detection = null,
    TargetDefinition? Definition = null);

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
    /// the bullet is not the hole. The detector's extent is the torn crown, which reaches about the calibre, not the bright aperture of about
    /// 0.68 of it. Only the split's veto reads it, a separation between one hole and two, never an absolute size.
    /// <para>
    /// <b>One figure, not two, NOTES-FROM-PLANNING.md entry 94 section 3.</b> It was two, 0.944 on a scan and 0.986 in a photograph, and the
    /// photograph figure was measured with one scale applied to an image whose true scale varies by up to 30 percent across a sheet. Re-measured
    /// at each blob's own scale (entry 84 section 3) the photographs read 0.948 with a spread of 0.039 where they had read 0.986 with 0.110, and
    /// the scans, which had no scale variation to fix, read 0.938. <b>The two agree within 0.010 and the spread is four times that</b>, so the
    /// difference was the defect rather than a property of the two media, and one constant is what the measurement supports: 102 holes over
    /// eight frames of two sheets, pooled 0.945, sheet means 0.949 and 0.932, frame means 0.918 to 0.976.
    /// </para>
    /// <para>
    /// The branch that went with the two figures is gone too: a photograph carrying no camera data was read as a scan, which mattered only
    /// while the two differed. The sheet is the unit of uncertainty (entry 80 section 3), and two sheets are enough to tell one hole from two
    /// and not enough to report a size back to a person. If a later measurement separates the media, split the constant again with the evidence
    /// attached.
    /// </para>
    /// <para>
    /// <b>A later measurement did separate them, and it says this constant does not describe a photograph at all (question 38, 2026-09-22).</b>
    /// Measured over 176 holes on nine photographs of four sheets of known calibre, the ratio runs from 0.90 to 1.45, sheet by sheet, while
    /// the scans of those same four sheets read 0.76, 0.92, 0.94 and 0.95. Photographs of one sheet agree with each other to within about 0.10;
    /// photographs of different sheets do not agree at all, and the spread within a photograph is three to ten times the spread within a scan.
    /// So there is no second constant to split off: what a hole measures in a photograph is a property of that photograph's light and paper,
    /// not of the medium. Nothing here changed, because a change needs evidence about scans and this is evidence about photographs; what a
    /// photograph needs instead is the sheet's own marks, which <see cref="Detection.HoleSizeSource.Sheet"/> already provides.
    /// </para>
    /// <para>
    /// What these figures are good for, entry 81 section 4: telling one hole from two is a factor-of-two judgement, and an error of a few
    /// percent in them cannot flip it, so two sheets are enough for that. They are not enough for anything that needs the absolute size, such
    /// as reporting a measured calibre back to a person or comparing hole sizes between loads. A use of that kind must measure the ratio on
    /// many more sheets first rather than inherit a precision these never had.
    /// </para>
    /// <para>
    /// <b>And a scan has now measured it on the other side of 1 (entry 161, 2026-09-24).</b> A friend's ten 6.5 Creedmoor shots at about
    /// 2845 fps measure 0.301 in across the middle on a 600 dpi scan, 1.14 times the 0.264 in bullet, where the range scans read 0.765 to
    /// 0.949. Imaging does not explain it, since both are scans. So the ratio is not a constant, and this figure no longer sets the reference
    /// the doubles test uses on any sheet with five marks or more: the sheet's own marks do. What it still does is keep a single small hole
    /// from being split and set the smallest hole accepted, which only need it to be roughly right. It is deliberately not replaced with
    /// 1.14, because one wrong constant replaced by another is the mistake the finding argues against.
    /// </para>
    /// </summary>
    public const double HoleToCalibre = 0.945;

    /// <summary>
    /// The range a hole has measured over the bullet that made it, on scans of sheets of known calibre: 0.76 on the .22 LR range scan, 1.14
    /// on the friend's card stock over cardboard. NOTES-FROM-PLANNING.md entry 162 section 3: the ratio depends on the paper and the backing
    /// at least, with velocity and the bullet's nose not yet separable from them, so no constant describes it and none replaces
    /// <see cref="HoleToCalibre"/>.
    /// <para>
    /// Where a calibre is still used without the sheet's own marks, it has to hold across this whole range, and it does, which
    /// <c>CalibreRangeTests</c> holds: the smallest hole accepted is well below the low end, and a single hole at the high end is still
    /// too small to be split. The calibre judges nothing else, because nothing else survives a range this wide.
    /// </para>
    /// </summary>
    public const double HoleToCalibreLow = 0.76;

    /// <inheritdoc cref="HoleToCalibreLow"/>
    public const double HoleToCalibreHigh = 1.14;

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
    /// <param name="artefacts">
    /// Keep each stage's picture for the timeline, DESIGN.md section 19 [r3]: on for one interactive analysis and off for batch, so the theatre
    /// never slows the pipeline down. Off by default; only the marking screen turns it on. It sets <see cref="TraceRecorder.KeepArtefacts"/>,
    /// so each stage's record carries its picture as it files and a live run shows it as the stage lands.
    /// </param>
    public static AutomaticResult Run(GrayImage grey, GrayImage value, ImageMetadata metadata, TargetDefinition definition, IImagingBackend backend, Trace.TraceRecorder? trace = null, CancellationToken cancellation = default, Calibre? calibre = null, bool artefacts = false, Measurement.MeasureOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellation.ThrowIfCancellationRequested();
        trace ??= new Trace.TraceRecorder();
        trace.KeepArtefacts |= artefacts;
        var measurement = SheetMeasurer.Measure(grey, metadata, definition, options ?? new MeasureOptions(), backend, trace);
        cancellation.ThrowIfCancellationRequested();
        var fiducials = measurement.Fiducials;
        string markers = fiducials is null ? "no markers" : string.Create(CultureInfo.InvariantCulture, $"{fiducials.Matches.Count} of {fiducials.Expected} markers found");

        // NOTES-FROM-PLANNING.md entry 130 section 2c: where more than one sheet is in the frame, the summary says so and says which one
        // was measured. It goes in the first clause because every figure below it is about that one sheet, and a person who photographed
        // two targets at once has no other way to tell which one they are reading.
        if (fiducials is { SheetsInView: > 1, WhichSheet: { } whichSheet })
        {
            markers += ". " + whichSheet;
        }
        if (measurement.Registration is not { } registration || fiducials is null)
        {
            // NOTES-FROM-PLANNING.md entry 120 section 1: the definition travels with a failure too. Without it the screen cannot say
            // which sheet it was trying to read, so entry 115 section 4's advice falls back to the raw failure, which is the one case
            // the advice was written for.
            return new AutomaticResult(measurement, null, [], [], [], markers, measurement.Failure ?? "registration failed", Definition: definition);
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
                detection = new DetectionRecord(calibre, calibre?.DiameterInches * HoleToCalibre);
                holes = RenderDifferenceHoleDetector.Detect(value, definition, fiducials.TileIndex, mapping, dpi, backend, new RenderDifferenceOptions(CalibreInches: calibre?.DiameterInches * HoleToCalibre, KeepResidual: trace.KeepArtefacts));
            }
            catch (InvalidOperationException ex)
            {
                // DESIGN.md section 19: the trace is never the only place an error appears, so the failure is returned as well as recorded.
                stage.Done(StageStatus.Failed, ex.Message);
                return new AutomaticResult(measurement, null, [], [], missing, markers, "hole detection failed: " + ex.Message, Definition: definition);
            }

            if (GroupLab.Core.Gltd.Validation.GltdValidator.Validate(definition).Any(d => d.Severity == GroupLab.Core.Gltd.Severity.Error))
            {
                stage.Decide("expected artwork", "rendered although the definition fails validation", "the sheet is already printed, and what was printed is what must be differenced", "refuse the sheet");
            }

            stage.Parameter("resolution", string.Create(CultureInfo.InvariantCulture, $"{dpi:0.0} px per inch, from the registration"));
            if (holes.HoleSize is { } holeSize)
            {
                stage.Parameter("hole size", holeSize.Description);
                if (holeSize.Source is HoleSizeSource.TwoSizes or HoleSizeSource.SheetTentative)
                {
                    stage.Detail(holeSize.Description);
                }
            }

            stage.Parameter("calibre", calibre is null ? "none named, so a single hole is the sheet's own 25th percentile mark once it has five, and shape alone decides before that" : string.Create(CultureInfo.InvariantCulture,
                $"{calibre.Name}, {calibre.DiameterInches:0.000} in; a hole of it has measured {calibre.DiameterInches * HoleToCalibreLow:0.000} to {calibre.DiameterInches * HoleToCalibreHigh:0.000} in on the scans so far, depending on the paper and the backing, so it keeps a single hole from being split and sets the smallest hole accepted, and does not judge one hole from two"));
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
            stage.Artefact(() => holes.Residual is { } residual ? new ResidualArtefact(residual) : null);
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
            stage.Decide("assignment", assignment.Method.Words(), assignment.Reason);
            foreach (var s in assignment.Shots.Where(s => s.Ambiguous))
            {
                stage.Detail(string.Create(CultureInfo.InvariantCulture,
                    $"shot {s.Shot + 1} ambiguous: given bull {s.Bull?.ToString(CultureInfo.InvariantCulture) ?? "none"} at {s.Distance / 254:0.000} in, nearest bull {s.NearestBull} at {s.NearestDistance / 254:0.000} in, margin {s.Margin / 254:0.000} in"));
            }

            stage.Done(ambiguous > 0 || unassigned > 0 ? StageStatus.Degraded : StageStatus.Ok, string.Create(CultureInfo.InvariantCulture,
                $"{assignment.Shots.Count} shots to {bullPages.Count} bulls by {assignment.Method.Words()}{(ambiguous > 0 ? $", {ambiguous} ambiguous" : "")}{(unassigned > 0 ? $", {unassigned} unassigned" : "")}"));
        }

        var detections = holes.Holes.Select((h, i) => new DetectedShot(new PointD(h.X, h.Y), assignment.Shots[i], h.DiameterInches,
            h.Oversized ? new DetectedOversize(h.SizeHoles ?? 0, h.OversizeTentative, h.SplitA, h.SplitB) : null,
            h.SizeHoles is { } size && !h.PossibleMerge ? new MarkSize(size, h.SplitA, h.SplitB) : null)).ToList();
        var rejected = holes.Rejected.Select(r => new RejectedCandidate(new PointD(r.X, r.Y), r.DiameterInches, r.Reason)).ToList();

        string summary = string.Create(CultureInfo.InvariantCulture,
            $"{markers}, {detection.Describe()}{(holes.HoleSize is { Source: HoleSizeSource.TwoSizes or HoleSizeSource.SheetTentative } sheetSize ? "; " + sheetSize.Description : "")}, registration RMS {registration.RmsResidual / 254:0.0000} in over {registration.Markers} markers, {holes.Holes.Count} holes detected{(holes.InsideZones.Count > 0 ? $", {holes.InsideZones.Count} hole-sized candidate{(holes.InsideZones.Count == 1 ? "" : "s")} inside printed-matter zones not looked at" : "")}, assigned by {assignment.Method.Words()}{(string.IsNullOrWhiteSpace(assignment.Reason) ? "" : ": " + assignment.Reason)}");
        return new AutomaticResult(measurement, new SheetReference(mapping, summary) { MarkersFound = fiducials.Matches.Count, MarkersExpected = fiducials.Expected }, bulls, detections, missing, summary, null, holes.Expected, assignment, rejected, holes, detection, definition);
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
