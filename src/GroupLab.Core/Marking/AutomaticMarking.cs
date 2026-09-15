using System.Globalization;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Marking;

/// <summary>
/// What the automatic path hands the marking screen: the measurement with its trace, the registered scale, the bulls, the detected
/// shots with their assignment, a one-line summary, and the failure when there is one.
/// </summary>
public sealed record AutomaticResult(
    SheetMeasurement Measurement,
    SheetReference? Scale,
    IReadOnlyList<BullAim> Bulls,
    IReadOnlyList<(PointD Image, int? Bull)> Detections,
    IReadOnlyList<PointD> MissingMarkers,
    string Summary,
    string? Failure);

/// <summary>
/// The automatic path for a GroupLab sheet, as NOTES-FROM-PLANNING.md entry 21 section 3 frames it: a way of pre-filling the marks
/// the user then accepts, moves or deletes. Registration from the printed markers (stages S1 to S4), render-and-difference hole
/// detection (S5 to S8, M2.2), and assignment by one-to-one matching where the counts allow it (S9). Every result is a set of
/// ordinary marks, so nothing the pipeline decides is final.
/// </summary>
public static class AutomaticMarking
{
    /// <param name="grey">The image as grey, for the markers.</param>
    /// <param name="value">The image as HSV value, max(R, G, B), for the holes.</param>
    /// <param name="trace">
    /// Where each stage records what it did (DESIGN.md section 19 [r3]); registration's stages come from <see cref="SheetMeasurer"/>,
    /// and the hole detection and assignment stages are recorded here.
    /// </param>
    public static AutomaticResult Run(GrayImage grey, GrayImage value, ImageMetadata metadata, TargetDefinition definition, IImagingBackend backend, Trace.TraceRecorder? trace = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        trace ??= new Trace.TraceRecorder();
        var measurement = SheetMeasurer.Measure(grey, metadata, definition, new MeasureOptions(), backend, trace);
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
        using (var stage = trace.Begin("S5-S8.holes"))
        {
            try
            {
                holes = RenderDifferenceHoleDetector.Detect(value, definition, fiducials.TileIndex, mapping, dpi, backend);
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
            stage.Metric("ink fraction", holes.InkFraction, "of paper");
            stage.Metric("holes", holes.Holes.Count, "count");
            stage.Metric("rejected", holes.Rejected.Count, "count");
            foreach (var r in holes.Rejected)
            {
                var page = mapping.ToPage(new PointD(r.X, r.Y));
                stage.Reject(string.Create(CultureInfo.InvariantCulture, $"blob {r.DiameterInches:0.000} in across"), r.Reason, Trace.PointInches.FromDmm(page.X, page.Y));
            }

            int merges = holes.Holes.Count(h => h.PossibleMerge), oversized = holes.Holes.Count(h => h.Oversized);
            stage.Done(StageStatus.Ok, string.Create(CultureInfo.InvariantCulture,
                $"{holes.Holes.Count} holes inside the registered sheet, {holes.Rejected.Count} candidates rejected{(merges > 0 ? $", {merges} from split merges" : "")}{(oversized > 0 ? $", {oversized} oversized" : "")}"));
        }

        var bulls = measurement.Bulls.Select(b => new BullAim(b.Index, b.Name, mapping.ToImage(b.Recovered ?? b.Declared), definition.Bulls[b.Index].Scoring)).ToList();
        var bullPages = definition.Bulls.Select(b => new PointD(b.X, b.Y)).ToList();
        var shotPages = holes.Holes.Select(h => mapping.ToPage(new PointD(h.X, h.Y))).ToList();
        ShotAssignmentResult assignment;
        using (var stage = trace.Begin("S9.assign"))
        {
            assignment = ShotAssignment.Assign(shotPages, bullPages);
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

        var detections = holes.Holes.Select((h, i) => (new PointD(h.X, h.Y), assignment.Shots[i].Bull)).ToList();

        string summary = string.Create(CultureInfo.InvariantCulture,
            $"{markers}, registration RMS {registration.RmsResidual / 254:0.0000} in over {registration.Markers} markers, {holes.Holes.Count} holes detected, assigned by {assignment.Method}: {assignment.Reason}");
        return new AutomaticResult(measurement, new SheetReference(mapping, summary), bulls, detections, missing, summary, null);
    }
}

/// <summary>
/// DESIGN.md section 13: rough clicks snap to the local centroid, so manual placement is not limited by mouse or finger precision.
/// A hole is darker than the paper around it, so the snap is the darkness-weighted centroid of the pixels within the radius that
/// are clearly darker than the window's paper level, taken twice, the second time about the first.
/// </summary>
public static class Snapping
{
    public static PointD ToDarkCentroid(GrayImage value, PointD click, double radiusPixels)
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

            int paper = 255;
            for (int level = 0, seen = 0; level < 256; level++)
            {
                seen += histogram[level];
                if (seen >= 0.9 * count)
                {
                    paper = level;
                    break;
                }
            }

            double sw = 0, sx = 0, sy = 0;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (((x - centre.X) * (x - centre.X)) + ((y - centre.Y) * (y - centre.Y)) > radiusPixels * radiusPixels)
                    {
                        continue;
                    }

                    double w = paper - value.Pixels[(y * value.Width) + x] - 40;
                    if (w > 0)
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
