using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>One sample measured once, with both bull locators run against the same registration.</summary>
public sealed record SampleResult(
    SampleSet.Sample Sample,
    FiducialResult Fiducials,
    RegistrationFit? Fit,
    ScaleReport? Scale,
    LensReport? Lens,
    IReadOnlyList<BullLocation> Centroid,
    IReadOnlyList<BullLocation> EdgeFit,
    string? Failure);

/// <summary>
/// The measurements of PHASE0-SPIKE-BRIEF.md sections 2 and 6 over the committed sample set, printed as Markdown tables
/// for the report. Command line only, per section 4.
/// </summary>
public static class Phase0Spike
{
    /// <summary>The paper and photograph gates of PHASE0-SPIKE-BRIEF.md section 2: 0.005 in worst bull-centre error, in dmm.</summary>
    public const double PaperGate = 1.27;

    private static readonly Dictionary<string, TargetDefinition> Definitions = new(StringComparer.Ordinal);

    public static int Sheets(string scans, string targets, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var backend = new OpenCvSharpBackend();
        output.WriteLine("| Image | DPI | Tile | Markers | Residual RMS / max (in) | Centroid mean / worst (in) | Edge fit mean / worst (in) | Ink spread (mm) | Scale x / y | Gate, centroid / edge |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var sample in SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan))
        {
            output.WriteLine(Row(Measure(scans, targets, sample, backend, new MeasureOptions())));
        }

        return 0;
    }

    public static SampleResult Measure(string scans, string targets, SampleSet.Sample sample, IImagingBackend backend, MeasureOptions options)
    {
        ArgumentNullException.ThrowIfNull(sample);
        ArgumentNullException.ThrowIfNull(options);
        var (image, metadata) = ImageLoader.Load(Path.Combine(scans, sample.File));
        var definition = Definition(targets, sample.Definition);
        var trace = new TraceRecorder();
        var fiducials = SheetMeasurer.DetectFiducials(image, metadata, definition, options, backend, trace);
        if (fiducials.Matches.Count < 4)
        {
            return new SampleResult(sample, fiducials, null, null, null, [], [], "too few markers to register");
        }

        var fit = SheetMeasurer.Register(image, metadata, fiducials, options, backend, trace);
        if (fit is null)
        {
            return new SampleResult(sample, fiducials, null, null, null, [], [], "registration failed");
        }

        var (scale, lens) = SheetMeasurer.VerifyScale(metadata, definition, options, fit, image, trace);
        var centroid = SheetMeasurer.LocateBulls(image, definition, fit.Mapping, options with { Locator = BullLocatorKind.Centroid }, trace);
        var edge = SheetMeasurer.LocateBulls(image, definition, fit.Mapping, options with { Locator = BullLocatorKind.EdgeFit }, trace);
        string? failure = fiducials.TileIndex == sample.Tile
            ? null
            : string.Create(CultureInfo.InvariantCulture, $"tile {fiducials.TileIndex + 1} inferred, tile {sample.Tile + 1} named");
        return new SampleResult(sample, fiducials, fit, scale, lens, centroid, edge, failure);
    }

    public static TargetDefinition Definition(string targets, string file)
    {
        lock (Definitions)
        {
            if (!Definitions.TryGetValue(file, out var definition))
            {
                definition = GltdJsonReader.ReadFile(Path.Combine(targets, file)).Definition
                    ?? throw new InvalidDataException($"{file} is not a valid definition.");
                Definitions[file] = definition;
            }

            return definition;
        }
    }

    /// <summary>Mean and worst error over the located bulls, in dmm, and how many were not located.</summary>
    public static (double Mean, double Worst, int Missing) Stats(IReadOnlyList<BullLocation> bulls)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        var found = bulls.Where(b => b.Recovered is not null).ToList();
        return found.Count == 0
            ? (double.NaN, double.NaN, bulls.Count)
            : (found.Average(b => b.Error), found.Max(b => b.Error), bulls.Count - found.Count);
    }

    private static string Row(SampleResult r)
    {
        var inv = CultureInfo.InvariantCulture;
        string dpi = r.Sample.Dpi is { } d ? d.ToString(inv) : "photo";
        if (r.Fit is null || r.Scale is null)
        {
            return string.Create(inv, $"| `{r.Sample.File}` | {dpi} | {r.Fiducials.TileIndex + 1} | {r.Fiducials.Matches.Count}/{r.Fiducials.Expected} | {r.Failure} | | | | | |");
        }

        var c = Stats(r.Centroid);
        var e = Stats(r.EdgeFit);
        double spread = r.EdgeFit.Where(b => b.InkSpread is not null).Select(b => b.InkSpread!.Value).DefaultIfEmpty(double.NaN).Average();
        string gate = r.Sample.Gated ? $"{Verdict(c)} / {Verdict(e)}" : "not gated";
        string scale = r.Scale.ScaleX is { } sx ? string.Create(inv, $"{sx:0.00000} / {r.Scale.ScaleY:0.00000}") : string.Create(inv, $"{r.Scale.PixelsPerDmmArea * 254:0} px/in");
        string note = r.Failure is null ? "" : $" ({r.Failure})";
        return string.Create(inv,
            $"| `{r.Sample.File}`{note} | {dpi} | {r.Fiducials.TileIndex + 1} | {r.Fiducials.Matches.Count}/{r.Fiducials.Expected} | {r.Fit.RmsResidual / 254:0.00000} / {r.Fit.MaxResidual / 254:0.00000} | {Pair(c)} | {Pair(e)} | {spread / 10:+0.000;-0.000} | {scale} | {gate} |");

        static string Pair((double Mean, double Worst, int Missing) s) =>
            string.Create(CultureInfo.InvariantCulture, $"{s.Mean / 254:0.00000} / {s.Worst / 254:0.00000}") + (s.Missing > 0 ? $", {s.Missing} not located" : "");

        static string Verdict((double Mean, double Worst, int Missing) s) => s.Missing == 0 && s.Worst < PaperGate ? "pass" : "FAIL";
    }
}
