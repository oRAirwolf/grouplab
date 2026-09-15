using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab spike corners --export &lt;file&gt;</c> and <c>--replay &lt;file&gt;</c>: the rerun from another platform's corners that
/// NOTES-FROM-PLANNING.md entry 49 section 2 asks for, and entry 52 section 5 item 4 orders. The <c>markers</c> and <c>refinement</c>
/// tables are the two the macOS runner prints differently, and this says which stage that difference enters at.
/// <list type="bullet">
/// <item><b>Export</b> reruns those two measurements and records every call the detector was asked to make, in order: the image it was
/// given, by hash, the settings it was given, and the corners it returned, at full precision.</item>
/// <item><b>Replay</b> reruns the same two measurements on this platform, but hands each detection back from the journal instead of
/// detecting, and compares the printed tables with the committed Windows tables. Everything downstream of detection, the homography, the
/// warp and the bull fit, still runs here.</item>
/// </list>
/// The journal's image hash separates the two things a difference could be: an input this platform built differently, which the synthetic
/// rasters are warped by the same native library that detects, or corners this platform's detector refined differently. Three outcomes
/// answer entry 49 section 2's question. Identical hashes and identical tables mean detection was the only divergence. Identical hashes
/// and differing tables mean there is a second divergence below detection. A differing hash means the difference is upstream of both, in
/// the image itself, and that call's corners are replayed anyway so the rest of the comparison still holds.
/// <para>
/// Nothing here gates. It reports, and the gate record above is what fails (entry 49 section 1).
/// </para>
/// </summary>
public static class CornerJournal
{
    /// <summary>The measurements whose printed tables differ on macOS, and the only ones this replays.</summary>
    public static readonly string[] Measurements = ["markers", "refinement"];

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,

        // Written on one platform and read on another, as RawMeasurements is, so the newline is stated rather than the platform's.
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static int Export(string scans, string targets, string file, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var real = new OpenCvSharpBackend();
        var calls = new List<Call>();
        try
        {
            foreach (string measurement in Measurements)
            {
                Phase0Measurements.Backend = new Recording(real, measurement, calls);
                Run(measurement, scans, targets, TextWriter.Null);
            }
        }
        finally
        {
            Phase0Measurements.Backend = real;
        }

        var document = new Journal(Environment.OSVersion.Platform.ToString(), Measurements, calls);
        File.WriteAllText(file, JsonSerializer.Serialize(document, Json) + "\n");
        output.WriteLine(string.Create(Inv, $"{calls.Count} detections recorded to {Path.GetFileName(file)}, {calls.Sum(c => c.Detection.Markers.Length)} markers over {Measurements.Length} measurements."));
        foreach (var group in calls.GroupBy(c => c.Measurement, StringComparer.Ordinal))
        {
            output.WriteLine(string.Create(Inv, $"  {group.Key}: {group.Count()} detections, {group.Select(c => c.ImageHash).Distinct(StringComparer.Ordinal).Count()} distinct images"));
        }

        return 0;
    }

    public static int Replay(string scans, string targets, string file, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var journal = JsonSerializer.Deserialize<Journal>(File.ReadAllText(file), Json)
            ?? throw new InvalidDataException($"{file} is not a corner journal.");
        var real = new OpenCvSharpBackend();
        var rows = new List<string>
        {
            "| Measurement | Detections replayed | Images identical to Windows | Printed table against the committed Windows table |",
            "|---|---|---|---|",
        };
        var notes = new List<string>();
        try
        {
            foreach (string measurement in Measurements)
            {
                var calls = journal.Calls.Where(c => string.Equals(c.Measurement, measurement, StringComparison.Ordinal)).ToList();
                var replaying = new Replaying(real, calls);
                Phase0Measurements.Backend = replaying;
                var printed = new StringWriter(Inv) { NewLine = "\n" };
                Run(measurement, scans, targets, printed);
                var here = Lines(printed.ToString());
                var committed = Lines(File.ReadAllText(Path.Combine(scans, "measurements", "tables", measurement + ".txt")));
                var differences = here.Count == committed.Count
                    ? [.. Enumerable.Range(0, here.Count).Where(i => !string.Equals(here[i], committed[i], StringComparison.Ordinal))]
                    : new List<int>();
                string table = here.Count != committed.Count
                    ? string.Create(Inv, $"**{committed.Count} lines committed, {here.Count} printed here**")
                    : differences.Count == 0 ? "identical" : string.Create(Inv, $"**differs**, {differences.Count} lines");
                rows.Add(string.Create(Inv,
                    $"| `{measurement}` | {replaying.Replayed} of {calls.Count}{(replaying.Unrecorded > 0 ? $", {replaying.Unrecorded} not in the journal" : "")} | {replaying.SameImage} of {replaying.Replayed} | {table} |"));
                foreach (int i in differences)
                {
                    notes.Add($"{measurement}, line {i + 1}:");
                    notes.Add($"  windows: {committed[i]}");
                    notes.Add($"  here:    {here[i]}");
                }

                if (replaying.DifferentImages.Count > 0)
                {
                    notes.Add(string.Create(Inv, $"{measurement}: {replaying.DifferentImages.Count} images differ from Windows' before detection, at call {string.Join(", ", replaying.DifferentImages.Take(8))}"));
                }
            }
        }
        finally
        {
            Phase0Measurements.Backend = real;
        }

        foreach (string row in rows)
        {
            output.WriteLine(row);
        }

        if (notes.Count > 0)
        {
            output.WriteLine();
            foreach (string note in notes)
            {
                output.WriteLine(note);
            }
        }

        return 0;
    }

    private static int Run(string measurement, string scans, string targets, TextWriter output) => measurement switch
    {
        "markers" => Phase0Measurements.MarkerCount(scans, targets, output),
        "refinement" => Phase0Measurements.Refinement(scans, targets, output),
        _ => throw new ArgumentOutOfRangeException(nameof(measurement), measurement, "Only the markers and refinement measurements are replayed."),
    };

    private static List<string> Lines(string text) => [.. text.Replace("\r", "", StringComparison.Ordinal).TrimEnd('\n').Split('\n')];

    private static string Hash(GrayImage image)
    {
        byte[] hash = SHA256.HashData(image.Pixels);
        return string.Create(Inv, $"{image.Width}x{image.Height}:{Convert.ToHexString(hash)[..16]}");
    }

    private static string Settings(MarkerDetectionOptions options) => string.Create(Inv,
        $"{options.Family}, side {options.ExpectedMarkerSidePixels:0.000000} px, {options.Refinement}, window {options.RefinementWindowModules?.ToString("0.####", Inv) ?? "default"}, threshold {options.ThresholdWindowMaxPixels?.ToString(Inv) ?? "sized"}, downsample {options.DownsampleFactor}");

    private static Detection Record(MarkerDetection detection) => new(
        [.. detection.Markers.Select(m => new RecordedMarker(m.Id, Flatten(m.Corners)))],
        [.. detection.Rejected.Select(r => new RecordedRejection(r.Id, Flatten(r.Corners), r.Reason))],
        [.. detection.Undecoded.Select(Flatten)]);

    private static MarkerDetection Restore(Detection detection) => new(
        [.. detection.Markers.Select(m => new DetectedMarker(m.Id, Points(m.Corners)))],
        [.. detection.Rejected.Select(r => new MarkerRejection(r.Id, Points(r.Corners), r.Reason))],
        [.. detection.Undecoded.Select(q => (IReadOnlyList<PointD>)Points(q))]);

    private static double[] Flatten(IReadOnlyList<PointD> corners) => [.. corners.SelectMany(p => (double[])[p.X, p.Y])];

    private static PointD[] Points(double[] flat) => [.. Enumerable.Range(0, flat.Length / 2).Select(i => new PointD(flat[i * 2], flat[(i * 2) + 1]))];

    private sealed record Journal(string Platform, IReadOnlyList<string> Measurements, IReadOnlyList<Call> Calls);

    private sealed record Call(int Index, string Measurement, string ImageHash, string Settings, Detection Detection);

    private sealed record Detection(RecordedMarker[] Markers, RecordedRejection[] Rejected, double[][] Undecoded);

    private sealed record RecordedMarker(int Id, double[] Corners);

    private sealed record RecordedRejection(int Id, double[] Corners, string Reason);

    /// <summary>The imaging backend, with every detection it performs written to the journal in the order it was asked for.</summary>
    private sealed class Recording(IImagingBackend inner, string measurement, List<Call> calls) : Wrapper(inner)
    {
        public override MarkerDetection DetectMarkers(GrayImage image, MarkerDetectionOptions options)
        {
            var detection = base.DetectMarkers(image, options);
            calls.Add(new Call(calls.Count(c => string.Equals(c.Measurement, measurement, StringComparison.Ordinal)), measurement, Hash(image), Settings(options), Record(detection)));
            return detection;
        }
    }

    /// <summary>
    /// The imaging backend, except that each detection is handed back from the journal in the order the calls are made, and this platform's
    /// detector is not asked. A call the journal does not hold falls through to the real detector and is counted, so a run that does not line
    /// up with the journal says so rather than quietly measuring something else.
    /// </summary>
    private sealed class Replaying(IImagingBackend inner, IReadOnlyList<Call> calls) : Wrapper(inner)
    {
        private int index;

        public int Replayed { get; private set; }

        public int Unrecorded { get; private set; }

        public int SameImage { get; private set; }

        public List<int> DifferentImages { get; } = [];

        public override MarkerDetection DetectMarkers(GrayImage image, MarkerDetectionOptions options)
        {
            if (index >= calls.Count)
            {
                Unrecorded++;
                return base.DetectMarkers(image, options);
            }

            var call = calls[index++];
            if (!string.Equals(call.Settings, Settings(options), StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Call {call.Index} of {call.Measurement} was recorded with {call.Settings}, and this run asked for {Settings(options)}.");
            }

            Replayed++;
            if (string.Equals(call.ImageHash, Hash(image), StringComparison.Ordinal))
            {
                SameImage++;
            }
            else
            {
                DifferentImages.Add(call.Index);
            }

            return Restore(call.Detection);
        }
    }

    /// <summary>Everything but detection, passed through to the real backend, so only the corners come from elsewhere.</summary>
    private abstract class Wrapper(IImagingBackend inner) : IImagingBackend
    {
        public virtual MarkerDetection DetectMarkers(GrayImage image, MarkerDetectionOptions options) => inner.DetectMarkers(image, options);

        public HomographyFit FindHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, double ransacThreshold) =>
            inner.FindHomography(source, destination, ransacThreshold);

        public GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height) => inner.WarpPerspective(image, transform, width, height);

        public GrayImage Morphology(GrayImage image, MorphologyOperation operation, int radius) => inner.Morphology(image, operation, radius);

        public IReadOnlyList<ImageBlob> FilledBlobs(GrayImage binary) => inner.FilledBlobs(binary);

        public (PointD Shift, double Response) PhaseCorrelate(GrayImage reference, GrayImage moved) => inner.PhaseCorrelate(reference, moved);

        public IReadOnlyList<byte[]> ReadCodes(GrayImage image, double scale) => inner.ReadCodes(image, scale);
    }
}
