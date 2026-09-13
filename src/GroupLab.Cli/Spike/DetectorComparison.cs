using System.Globalization;
using System.Text.Json;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// Measurement 8 of FIDUCIAL-DECISION.md section 10, measurement 4 of PHASE0-SPIKE-BRIEF.md section 6, re-run through
/// the Phase 0 pipeline: OpenCV's corners detected here, and libapriltag's from <c>scans/phase0/apriltag-corners.json</c>
/// (NOTES-FROM-PLANNING.md entry 3), each through the same registration and both bull locators.
/// </summary>
public static class DetectorComparison
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>
    /// libapriltag's corners are converted to the model's top-left, top-right, bottom-right, bottom-left order by
    /// reversing the list, as entry 3 records; on sheet 1 marker 0 its fourth corner is the printed top-left. The two
    /// libraries' pixel conventions are not assumed equal: the mean offset between their corners for the same markers is
    /// reported, and libapriltag is also run shifted by half a pixel, so a convention difference cannot pass as a ranking.
    /// </summary>
    public static int Run(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var backend = new OpenCvSharpBackend();
        var definition = Phase0Spike.Definition(targets, SampleSet.CentreFire);
        double half = definition.Fiducials!.MarkerSize / 2.0;
        var printed = PageRegistration.ExpectedMarkers(definition, 0).ToDictionary(m => m.Id);
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(scans, "apriltag-corners.json")));

        var rows = new List<string>();
        var offsets = new List<string>();
        var raw = new List<object>();
        foreach (var image in document.RootElement.EnumerateObject().Where(p => !p.Name.StartsWith('_')))
        {
            var (pixels, metadata) = ImageLoader.Load(Path.Combine(scans, image.Name));
            var options = new MeasureOptions();
            var opencv = SheetMeasurer.DetectFiducials(pixels, metadata, definition, options, backend, new TraceRecorder());

            var detections = image.Value.EnumerateArray()
                .Select(d => (Id: d.GetProperty("id").GetInt32(), Corners: d.GetProperty("corners").EnumerateArray()
                    .Select(c => new PointD(c[0].GetDouble(), c[1].GetDouble())).Reverse().ToArray()))
                .Where(d => printed.ContainsKey(d.Id))
                .ToList();

            var cvById = opencv.Matches.ToDictionary(m => m.Id);
            var pairs = detections.Where(d => cvById.ContainsKey(d.Id))
                .SelectMany(d => d.Corners.Select((p, k) => (d.Id, Corner: k, Dx: p.X - cvById[d.Id].ImageCorners[k].X, Dy: p.Y - cvById[d.Id].ImageCorners[k].Y)))
                .ToList();
            double mx = pairs.Average(p => p.Dx), my = pairs.Average(p => p.Dy);
            double spread = Math.Sqrt(pairs.Average(p => Math.Pow(p.Dx - mx, 2) + Math.Pow(p.Dy - my, 2)));
            offsets.Add(string.Create(Inv, $"| `{image.Name}` | {detections.Count} | {mx:+0.000;-0.000} / {my:+0.000;-0.000} | {spread:0.000} |"));

            var detectors = new List<object>();
            var imageRows = new List<string> { Row("OpenCV, shipped settings", opencv) };
            foreach (var (name, shift) in (IEnumerable<(string, double)>)[("libapriltag, as returned", 0.0), ("libapriltag, minus half a pixel", -0.5)])
            {
                var matches = detections.Select(d => new MarkerMatch(d.Id,
                    [.. d.Corners.Select(p => new PointD(p.X + shift, p.Y + shift))],
                    [new(printed[d.Id].X - half, printed[d.Id].Y - half), new(printed[d.Id].X + half, printed[d.Id].Y - half),
                     new(printed[d.Id].X + half, printed[d.Id].Y + half), new(printed[d.Id].X - half, printed[d.Id].Y + half)])).ToList();
                var apriltag = opencv with
                {
                    Matches = matches,
                    Missing = [.. printed.Values.Where(m => matches.All(x => x.Id != m.Id))],
                    Undecoded = [],
                };
                imageRows.Add(Row(name, apriltag));
            }

            rows.AddRange(imageRows);
            raw.Add(new
            {
                file = image.Name,
                cornerConversion = "libapriltag's list reversed; the minus-half-a-pixel detector also subtracts 0.5 px from both coordinates",
                offsets = pairs.Select(p => new { markerId = p.Id, corner = p.Corner, dxPx = RawMeasurements.R(p.Dx), dyPx = RawMeasurements.R(p.Dy) }).ToArray(),
                detectors,
            });

            string Row(string detector, FiducialResult fiducials)
            {
                var trace = new TraceRecorder();
                var fit = SheetMeasurer.Register(pixels, metadata, fiducials, options, backend, trace);
                if (fit is null)
                {
                    detectors.Add(new { detector, failure = "registration failed" });
                    return $"| `{image.Name}` | {detector} | {fiducials.Matches.Count}/{fiducials.Expected} | registration failed | | |";
                }

                var centroid = SheetMeasurer.LocateBulls(pixels, definition, fit.Mapping, options with { Locator = BullLocatorKind.Centroid }, trace);
                var edge = SheetMeasurer.LocateBulls(pixels, definition, fit.Mapping, options with { Locator = BullLocatorKind.EdgeFit }, trace);
                detectors.Add(new
                {
                    detector,
                    residualRms = RawMeasurements.R(fit.RmsResidual),
                    residualMax = RawMeasurements.R(fit.MaxResidual),
                    mapping = RawMeasurements.Mapping(fit.Mapping),
                    corners = RawMeasurements.Corners(fit),
                    bulls = new { centroid = RawMeasurements.Bulls(centroid, definition), edgeFit = RawMeasurements.Bulls(edge, definition) },
                });
                var c = Phase0Spike.Stats(centroid);
                var e = Phase0Spike.Stats(edge);
                return string.Create(Inv,
                    $"| `{image.Name}` | {detector} | {fiducials.Matches.Count}/{fiducials.Expected} | {fit.RmsResidual / 254:0.00000} / {fit.MaxResidual / 254:0.00000} | {c.Mean / 254:0.00000} / {c.Worst / 254:0.00000} | {e.Mean / 254:0.00000} / {e.Worst / 254:0.00000} |");
            }
        }

        output.WriteLine("| Image | Detector | Markers | Residual RMS / max (in) | Centroid mean / worst (in) | Edge fit mean / worst (in) |");
        output.WriteLine("|---|---|---|---|---|---|");
        rows.ForEach(output.WriteLine);
        output.WriteLine();
        output.WriteLine("| Image | Markers in both | Mean offset, libapriltag minus OpenCV, x / y (px) | RMS about that mean (px) |");
        output.WriteLine("|---|---|---|---|");
        offsets.ForEach(output.WriteLine);
        RawMeasurements.Write(scans, "detectors", raw);
        return 0;
    }
}
