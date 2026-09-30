using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.Core.Capture;

namespace GroupLab.Mobile;

/// <summary>What the diagnostics overlay shows at one moment; a value the screen does not have is null and its line is left out.</summary>
internal sealed record OverlayState(
    double? FramesPerSecond = null,
    long? FrameMs = null,
    Instruction? Say = null,
    string? Failing = null,
    double? TiltDegrees = null,
    int? TorchLevel = null,
    int? TorchOf = null,
    string? Stage = null,
    TimeSpan? StageTook = null,
    TimeSpan? Elapsed = null,
    long? MemoryMb = null,
    long? HeapMb = null,
    string? Heat = null);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 4: "Show diagnostics on the camera", a setting on every build, off until turned on. A small block
/// of text over the camera (the analysis frame rate and each frame's time, the guidance's instruction and the check that holds it, the tilt,
/// the torch) and over the reading screen (the stage being read and its time, the reading's time), each with the memory in use and how hot
/// the phone says it is, so a screenshot alone explains a hang. It is built from values the screens already compute, and redrawn at most
/// every <see cref="EveryMs"/>.
/// </summary>
internal static class DiagnosticsOverlay
{
    /// <summary>The overlay is redrawn at most this often, in milliseconds: four times a second, which costs nothing a person would notice.</summary>
    public const long EveryMs = 250;

    /// <summary>Whether the overlay is wanted; read once as a screen opens, since the setting is a file.</summary>
    public static bool On => Phone.Settings.LoadShowDiagnostics();

    /// <summary>The overlay's lines, in a fixed order, every number written the same way whatever the phone's language.</summary>
    public static IReadOnlyList<string> Lines(OverlayState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var inv = CultureInfo.InvariantCulture;
        var lines = new List<string>();
        if (state.FramesPerSecond is { } fps)
        {
            lines.Add(string.Create(inv, $"frames {fps:0.0}/s") + (state.FrameMs is { } ms ? string.Create(inv, $", {ms} ms each") : ""));
        }

        if (state.Say is { } say)
        {
            lines.Add($"guidance {say}" + (state.Failing is { } failing ? $", held by {failing}" : ""));
        }

        if (state.TiltDegrees is { } tilt)
        {
            lines.Add(string.Create(inv, $"tilt {tilt:0.0} deg") + (tilt <= BubbleLevel.ReadyDegrees ? ", level" : ""));
        }

        if (state.TorchOf is { } of)
        {
            lines.Add(of == 0 ? "torch none" : string.Create(inv, $"torch {state.TorchLevel ?? 0} of {of}"));
        }

        if (state.Stage is { } stage)
        {
            lines.Add($"stage {stage}" + (state.StageTook is { } took ? string.Create(inv, $", {took.TotalSeconds:0.0} s") : ""));
        }

        if (state.Elapsed is { } elapsed)
        {
            lines.Add(string.Create(inv, $"reading {elapsed.TotalSeconds:0.0} s"));
        }

        if (state.MemoryMb is not null || state.HeapMb is not null || state.Heat is not null)
        {
            var parts = new List<string>();
            if (state.MemoryMb is { } memory)
            {
                parts.Add(string.Create(inv, $"memory {memory} MB"));
            }

            if (state.HeapMb is { } heap)
            {
                parts.Add(string.Create(inv, $"heap {heap} MB"));
            }

            if (state.Heat is { } heat)
            {
                parts.Add($"heat {heat}");
            }

            lines.Add(string.Join(", ", parts));
        }

        return lines;
    }

    public static string Text(OverlayState state) => string.Join('\n', Lines(state));

    /// <summary>
    /// The check that holds a frame back, in <see cref="CaptureGuidance.Judge"/>'s order, with the part of the score where it has one; null
    /// when the frame is ready. Where every check holds and the instruction is still not Ready, the steadier held it (a shake, or words not
    /// yet changed), and it says so.
    /// </summary>
    public static string? Failing(FrameVerdict verdict)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        var inv = CultureInfo.InvariantCulture;
        var q = verdict.Quality;
        return verdict.Say == Instruction.Ready ? null
            : !verdict.Detected ? "no sheet"
            : !verdict.SheetInFrame ? "out of frame"
            : q is null ? "not registered"
            : q.ResolutionPart < CaptureGuidance.Holds ? string.Create(inv, $"resolution {q.ResolutionPart:0.00}")
            : !verdict.AngleWithin ? string.Create(inv, $"angle {q.OffAxisDegrees:0.0} deg")
            : !verdict.InFocus ? string.Create(inv, $"focus {q.FocusPart:0.00}")
            : !verdict.ExposureWithin ? string.Create(inv, $"light {q.ExposurePart:0.00}")
            : !verdict.MarkingsRead ? string.Create(inv, $"markings {q.MarkingsPart:0.00}")
            : "steadiness";
    }

    /// <summary>The camera's state for the overlay, with the memory and the heat added here.</summary>
    public static string Camera(double? framesPerSecond, long frameMs, FrameVerdict verdict, double? tilt, int torchLevel, int torchOf) =>
        Text(new OverlayState(framesPerSecond, frameMs, verdict.Say, Failing(verdict), tilt, torchLevel, torchOf,
            MemoryMb: DeviceHealth.MemoryMb(), HeapMb: DeviceHealth.HeapMb(), Heat: DeviceHealth.HeatNow()));

    /// <summary>The reading screen's state for the overlay: the stage now, the reading's time, the memory and the heat.</summary>
    public static string Reading(TimeSpan elapsed)
    {
        var (stage, took) = ReadStage.Now;
        return Text(new OverlayState(Stage: stage, StageTook: stage is null ? null : took, Elapsed: elapsed,
            MemoryMb: DeviceHealth.MemoryMb(), HeapMb: DeviceHealth.HeapMb(), Heat: DeviceHealth.HeatNow()));
    }

    /// <summary>
    /// The reading screen <paramref name="page"/> with the overlay in its bottom corner where the setting is on, redrawn four times a second
    /// while it is shown; the page as it is where the setting is off.
    /// </summary>
    public static Control Over(Control page, Func<TimeSpan> elapsed)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(elapsed);
        if (!On)
        {
            return page;
        }

        var text = new TextBlock
        {
            FontFamily = new FontFamily("monospace"),
            FontSize = 12,
            Foreground = Brushes.White,
            Text = Reading(elapsed()),
            TextWrapping = TextWrapping.Wrap,
        };
        var box = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(170, 0, 0, 0)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 6),
            Margin = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            IsHitTestVisible = false,
            Child = text,
        }.Id("diagnostics-overlay");
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(EveryMs) };
        timer.Tick += (_, _) => text.Text = Reading(elapsed());
        box.AttachedToVisualTree += (_, _) => timer.Start();
        box.DetachedFromVisualTree += (_, _) => timer.Stop();
        return new Grid { Children = { page, box } };
    }
}

/// <summary>The stage a reading is in now and when it began, for the overlay; one reading runs at a time.</summary>
internal static class ReadStage
{
    private sealed record At(string Stage, long Began);

    private static At? current;

    /// <summary>A stage begun, from the reading's own thread.</summary>
    public static void Began(string stage) => Volatile.Write(ref current, new At(stage, Stopwatch.GetTimestamp()));

    /// <summary>The stage now and how long it has run; no stage before the first reading.</summary>
    public static (string? Stage, TimeSpan Took) Now => Volatile.Read(ref current) is { } at ? (at.Stage, Stopwatch.GetElapsedTime(at.Began)) : (null, TimeSpan.Zero);
}

/// <summary>
/// Entry 315 section 4: the memory in use and how hot the phone says it is, for the reading's and the camera's log lines and the overlay.
/// The heat comes from the head: iOS's thermal state (nominal, fair, serious, critical) and Android's thermal status (none, light, moderate,
/// severe, critical, emergency, shutdown).
/// </summary>
internal static class DeviceHealth
{
    /// <summary>The phone's own word for its heat, set by the head before the start; none on the desktop and in the tests.</summary>
    public static Func<string?>? Heat { get; set; }

    /// <summary>The memory the process holds, in megabytes, where the system says; null where it does not.</summary>
    public static long? MemoryMb()
    {
        try
        {
            long bytes = Environment.WorkingSet;
            return bytes > 0 ? bytes / (1024 * 1024) : null;
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // On iOS the figure comes through the process's own record, which a sandbox may refuse; the line goes without it.
            return null;
        }
    }

    /// <summary>The managed heap, in megabytes: what GroupLab's own objects take, which a leak of pictures would grow.</summary>
    public static long HeapMb() => GC.GetTotalMemory(false) / (1024 * 1024);

    public static string? HeatNow()
    {
        try
        {
            return Heat?.Invoke();
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return null;
        }
    }

    /// <summary>The fields a log line carries: memMb, heapMb and heat.</summary>
    public static (string Key, object? Value)[] Fields() => [("memMb", MemoryMb()), ("heapMb", HeapMb()), ("heat", HeatNow())];
}

/// <summary>The analysis frames judged a second, counted over about a second at a time; nothing until the first second has passed.</summary>
internal sealed class FrameRate
{
    private long since = -1;
    private int count;
    private double? rate;

    /// <summary>One frame judged at <paramref name="nowMs"/>; the rate as last counted.</summary>
    public double? Next(long nowMs)
    {
        if (since < 0)
        {
            since = nowMs;
            return rate;
        }

        count++;
        long span = nowMs - since;
        if (span >= 1000)
        {
            rate = count * 1000.0 / span;
            (since, count) = (nowMs, 0);
        }

        return rate;
    }
}
