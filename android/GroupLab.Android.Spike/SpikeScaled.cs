using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;
using OpenCvSharp;

namespace GroupLab.Android.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 209 section 1.2, for entry 206 section 2.2: the engine on the sample brought to a working resolution, timed,
/// with its peak memory and every hole it found, in full resolution pixels, so the holes can be set against the full resolution run's.
/// Run once per fresh process, because the peak is the whole process's highest so far.
/// </summary>
public static class SpikeScaled
{
    public static string Run(string imagePath, IReadOnlyList<string> targetFolders, double scale)
    {
        var inv = CultureInfo.InvariantCulture;
        // A phone never decodes a 32 megapixel scan to work on 8 of them, so the smaller image is written once, in one process, and the
        // measurement is made in the next on the small file alone; otherwise the full decode sets the peak whatever the working size.
        string small = Path.Combine(Path.GetDirectoryName(imagePath)!, string.Create(inv, $"sample-{scale:0.####}.png"));
        if (scale < 1 && !File.Exists(small))
        {
            // In colour, as the scan is: the engine reads the brightest channel as well as the grey.
            using var full = Cv2.ImRead(imagePath, ImreadModes.Color);
            using var reduced = new Mat();
            Cv2.Resize(full, reduced, new Size(0, 0), scale, scale, InterpolationFlags.Area);
            Cv2.ImWrite(small, reduced);
            return string.Create(inv, $"scale {scale:0.###}: prepared {reduced.Width} by {reduced.Height}; run again in a fresh process to measure");
        }

        var clock = Stopwatch.StartNew();
        string source = scale < 1 ? small : imagePath;
        var (grey, metadata) = ImageLoader.Load(source);
        var (value, _) = ImageLoader.LoadMaxChannel(source);
        metadata = metadata with { DpiX = 600 * scale, DpiY = 600 * scale };

        long load = clock.ElapsedMilliseconds;
        var backend = new OpenCvSharpBackend();
        var identity = SheetIdentification.Identify(grey, SheetIdentification.Candidates(targetFolders), backend, new TraceRecorder());
        if (identity.Definition is not { } definition)
        {
            return string.Create(inv, $"scale {scale:0.###}: {grey.Width} by {grey.Height}, not named: {identity.Failure}");
        }

        var result = AutomaticMarking.Run(grey, value, metadata, definition, backend);
        long total = clock.ElapsedMilliseconds;
        using var process = Process.GetCurrentProcess();
        string holes = string.Join(";", result.Detections.Select(d => string.Create(inv, $"{d.Image.X / scale:0.0},{d.Image.Y / scale:0.0}")));
        return string.Create(inv,
            $"scale {scale:0.###}: {grey.Width} by {grey.Height} ({grey.Width * (double)grey.Height / 1e6:0.0} MP), load {load} ms, total {total} ms, {result.Detections.Count} holes{(result.Failure is null ? "" : $", failed: {result.Failure}")}, peak {process.PeakWorkingSet64 / 1048576} MB | holes {holes}");
    }
}
