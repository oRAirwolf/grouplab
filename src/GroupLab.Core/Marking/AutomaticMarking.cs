using System.Globalization;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;

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
    public static AutomaticResult Run(GrayImage grey, GrayImage value, ImageMetadata metadata, TargetDefinition definition, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var measurement = SheetMeasurer.Measure(grey, metadata, definition, new MeasureOptions(), backend);
        var fiducials = measurement.Fiducials;
        string markers = fiducials is null ? "no markers" : string.Create(CultureInfo.InvariantCulture, $"{fiducials.Matches.Count} of {fiducials.Expected} markers found");
        if (measurement.Registration is not { } registration || fiducials is null)
        {
            return new AutomaticResult(measurement, null, [], [], [], markers, measurement.Failure ?? "registration failed");
        }

        var mapping = registration.Mapping;
        var missing = fiducials.Missing.Select(m => mapping.ToImage(new PointD(m.X, m.Y))).ToList();
        double dpi = (measurement.Scale?.PixelsPerDmmArea ?? fiducials.PixelsPerDmm) * 254;
        var holes = RenderDifferenceHoleDetector.Detect(value, definition, fiducials.TileIndex, mapping, dpi, backend);

        var bulls = measurement.Bulls.Select(b => new BullAim(b.Index, b.Name, mapping.ToImage(b.Recovered ?? b.Declared))).ToList();
        var bullPages = definition.Bulls.Select(b => new PointD(b.X, b.Y)).ToList();
        var shotPages = holes.Holes.Select(h => mapping.ToPage(new PointD(h.X, h.Y))).ToList();
        var assignment = ShotAssignment.Assign(shotPages, bullPages);
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
