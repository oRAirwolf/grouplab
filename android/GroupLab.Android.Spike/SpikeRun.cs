using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Android.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 198 section 2.4: the desktop's own engine on one image, timed, exactly as the desktop runs it: load,
/// name the sheet from its codes, then the automatic marking. Plain .NET with nothing from Android in it, so the same file is timed on
/// the desktop for the comparison the entry asks for.
/// </summary>
public static class SpikeRun
{
    /// <summary>One line for the image: its size, each stage's time, what was found, and the process's peak memory so far.</summary>
    public static string Run(string imagePath, IReadOnlyList<string> targetFolders)
    {
        var inv = CultureInfo.InvariantCulture;
        string name = Path.GetFileName(imagePath);
        try
        {
            var clock = Stopwatch.StartNew();
            var (grey, metadata) = ImageLoader.Load(imagePath);
            var (value, _) = ImageLoader.LoadMaxChannel(imagePath);
            long load = clock.ElapsedMilliseconds;

            var backend = new OpenCvSharpBackend();
            var identity = SheetIdentification.Identify(grey, SheetIdentification.Candidates(targetFolders), backend, new TraceRecorder());
            long identify = clock.ElapsedMilliseconds - load;
            if (identity.Definition is not { } definition)
            {
                return string.Create(inv, $"{name}: {grey.Width} by {grey.Height}, load {load} ms, codes {identity.CodesRead} in {identify} ms, not named: {identity.Failure}. {Peak()}");
            }

            var result = AutomaticMarking.Run(grey, value, metadata, definition, backend);
            long detect = clock.ElapsedMilliseconds - load - identify;
            return string.Create(inv,
                $"{name}: {grey.Width} by {grey.Height}, load {load} ms, {identity.CodesRead} codes and named {definition.Name} in {identify} ms, " +
                $"marking {detect} ms, {result.Detections.Count} holes{(result.Failure is null ? "" : $", failed: {result.Failure}")}, total {clock.ElapsedMilliseconds} ms. {Peak()}");
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return $"{name}: {e.GetType().Name}: {e.Message}";
        }
    }

    /// <summary>The peak resident memory of the whole process, the figure a phone kills an application by.</summary>
    private static string Peak()
    {
        using var process = Process.GetCurrentProcess();
        return string.Create(CultureInfo.InvariantCulture, $"Peak memory {process.PeakWorkingSet64 / 1048576} MB.");
    }
}
