using System.Diagnostics;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Survey;

/// <summary>One stage of an analysis and how long it took.</summary>
public sealed record StageTime(string Stage, long Milliseconds);

/// <summary>What the benchmark measured: the same work on every machine, timed stage by stage.</summary>
public sealed record BenchmarkResult(string Workload, int Width, int Height, long TotalMilliseconds, IReadOnlyList<StageTime> Stages, long PeakMegabytes, int HolesPlaced, int HolesFound);

/// <summary>
/// docs/SURVEY.md section 3, NOTES-FROM-PLANNING.md entry 207 section 3: a fixed test built into the application, so every copy runs the
/// same work and the times can be compared. The sheet is GL-CF25-LTR rendered at 300 dpi with one hole in each of its 25 bulls, made the
/// same way each time from a fixed seed, then analyzed exactly as a photograph is. Nothing is read from or written to disk, and it never
/// starts by itself: the person starts it.
/// </summary>
public static class Benchmark
{
    /// <summary>The name of the work, sent with the times, so a change to the work is never mistaken for a change in speed.</summary>
    public const string Workload = "GL-CF25-LTR-300dpi-25-holes-1";

    /// <summary>The sheet the work is done on, by its file name in the library.</summary>
    public const string SheetFile = "GL-CF25-LTR.gltd.json";

    private const double Dpi = 300;

    /// <summary>The benchmark's image: the same pixels on every machine.</summary>
    public static (GrayImage Image, int Holes) Sheet(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        var random = new Random(207);
        bool OnInk(double x, double y) => render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128;
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => (X: b.X + (double)random.Next(-40, 41), Y: b.Y + (double)random.Next(-40, 41)))
            .Select(p => SyntheticSheet.SampleHole(random, p.X, p.Y, OnInk(p.X, p.Y), HoleBacking.ScannerLid, 0.871))
            .ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        return (SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random), holes.Count);
    }

    /// <summary>Runs the work once and times it. Only the analysis is timed; making the image is not part of what a person waits for.</summary>
    public static BenchmarkResult Run(TargetDefinition definition, IImagingBackend backend, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(backend);
        var (image, placed) = Sheet(definition);
        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var trace = new TraceRecorder();
        var clock = Stopwatch.StartNew();
        var result = AutomaticMarking.Run(image, image, metadata, definition, backend, trace, cancellation);
        clock.Stop();
        return new BenchmarkResult(Workload, image.Width, image.Height, clock.ElapsedMilliseconds, Stages(trace), PeakMegabytes(), placed, result.Detections.Count);
    }

    /// <summary>Each stage the analysis recorded and its time, in the order they ran.</summary>
    public static IReadOnlyList<StageTime> Stages(TraceRecorder trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        return [.. trace.Records.Select(r => new StageTime(r.Stage, r.DurationMs))];
    }

    /// <summary>The most memory the process has held, in megabytes.</summary>
    public static long PeakMegabytes()
    {
        using var self = Process.GetCurrentProcess();
        return self.PeakWorkingSet64 / (1024 * 1024);
    }
}
